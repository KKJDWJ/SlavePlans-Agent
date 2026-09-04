using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
namespace SlaveSplit.Repositories
{
    public sealed class LocalConversationRepository : IConversationRepository
    {
        private sealed class ConversationState { public Guid? LastConversationId { get; set; } }
        private readonly string conversationsPath, messagesPath, statePath; private readonly FileLogger logger; private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private readonly JsonSerializerSettings settings = new JsonSerializerSettings { Formatting = Formatting.Indented, DateFormatHandling = DateFormatHandling.IsoDateFormat };
        public LocalConversationRepository(string directory = null)
        {
            string root = directory ?? Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit"); Directory.CreateDirectory(root);
            conversationsPath = Path.Combine(root, "conversations.json"); messagesPath = Path.Combine(root, "messages.json"); statePath = Path.Combine(root, "conversation_state.json"); logger = new FileLogger(root);
        }
        public async Task<IReadOnlyList<Conversation>> GetConversationsAsync() { return await ReadAsync<Conversation>(conversationsPath, "Conversation Load 실패"); }
        public async Task<Conversation> GetConversationAsync(Guid id) { return (await GetConversationsAsync()).FirstOrDefault(x => x.Id == id); }
        public Task CreateConversationAsync(Conversation conversation) { return MutateAsync<Conversation>(conversationsPath, "Conversation Create 실패", list => { if (list.Any(x => x.Id == conversation.Id)) throw new InvalidOperationException("Duplicate conversation id."); list.Add(conversation); }); }
        public Task UpdateConversationAsync(Conversation conversation) { return MutateAsync<Conversation>(conversationsPath, "Conversation Update 실패", list => { int index = list.FindIndex(x => x.Id == conversation.Id); if (index < 0) throw new KeyNotFoundException("Conversation not found."); list[index] = conversation; }); }
        public async Task DeleteConversationAsync(Guid id)
        {
            await gate.WaitAsync(); try { List<Conversation> conversations = ReadUnsafe<Conversation>(conversationsPath, "Conversation Load 실패", true); List<Message> messages = ReadUnsafe<Message>(messagesPath, "Message Load 실패", true); conversations.RemoveAll(x => x.Id == id); messages.RemoveAll(x => x.ConversationId == id); WriteAtomic(conversationsPath, conversations); WriteAtomic(messagesPath, messages); } catch (Exception ex) { logger.Error("Conversation Delete 실패", ex); throw; } finally { gate.Release(); }
        }
        public async Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, int take = 100) { IReadOnlyList<Message> all = await ReadAsync<Message>(messagesPath, "Message Load 실패"); return all.Where(x => x.ConversationId == conversationId).OrderByDescending(x => x.CreatedAt).Take(take).OrderBy(x => x.CreatedAt).ToList(); }
        public Task AddMessageAsync(Message message) { return MutateAsync<Message>(messagesPath, "Message Save 실패", list => list.Add(message)); }
        public async Task<Guid?> GetLastConversationIdAsync() { await gate.WaitAsync(); try { if (!File.Exists(statePath)) return null; ConversationState state = JsonConvert.DeserializeObject<ConversationState>(File.ReadAllText(statePath)); return state == null ? null : state.LastConversationId; } catch (Exception ex) { logger.Error("Conversation State Load 실패", ex); return null; } finally { gate.Release(); } }
        public async Task SetLastConversationIdAsync(Guid id) { await gate.WaitAsync(); try { WriteAtomicObject(statePath, new ConversationState { LastConversationId = id }); } catch (Exception ex) { logger.Error("Conversation State Save 실패", ex); throw; } finally { gate.Release(); } }
        private async Task<IReadOnlyList<T>> ReadAsync<T>(string path, string operation) { await gate.WaitAsync(); try { return ReadUnsafe<T>(path, operation, false); } finally { gate.Release(); } }
        private List<T> ReadUnsafe<T>(string path, string operation, bool throwOnFailure) { if (!File.Exists(path)) return new List<T>(); try { return JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(path), settings) ?? new List<T>(); } catch (Exception ex) { logger.Error(operation + " File=" + path, ex); if (throwOnFailure) throw; return new List<T>(); } }
        private async Task MutateAsync<T>(string path, string operation, Action<List<T>> mutate) { await gate.WaitAsync(); try { List<T> list = ReadUnsafe<T>(path, operation, true); mutate(list); WriteAtomic(path, list); } catch (Exception ex) { logger.Error(operation, ex); throw; } finally { gate.Release(); } }
        private void WriteAtomic<T>(string path, List<T> value) { WriteAtomicText(path, JsonConvert.SerializeObject(value, settings)); }
        private void WriteAtomicObject(string path, object value) { WriteAtomicText(path, JsonConvert.SerializeObject(value, settings)); }
        private void WriteAtomicText(string path, string json)
        {
            string temp = path + ".tmp", backup = path + ".bak"; try { using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) using (StreamWriter writer = new StreamWriter(stream)) { writer.Write(json); writer.Flush(); stream.Flush(true); } if (File.Exists(path)) File.Replace(temp, path, backup, true); else File.Move(temp, path); } catch { try { if (File.Exists(temp)) File.Delete(temp); } catch { } throw; }
        }
    }
}
