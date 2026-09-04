using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Repositories;
namespace SlaveSplit.Services
{
    public sealed class ConversationService
    {
        private readonly IConversationRepository repository; private readonly IClock clock;
        public event EventHandler ConversationsChanged;
        public ConversationService(IConversationRepository repository, IClock clock = null) { this.repository = repository; this.clock = clock ?? new SystemClock(); }
        public Task<IReadOnlyList<Conversation>> GetConversationsAsync() { return repository.GetConversationsAsync(); }
        public Task<IReadOnlyList<Message>> GetMessagesAsync(Guid id, int take = 100) { return repository.GetMessagesAsync(id, take); }
        public async Task<IReadOnlyList<Message>> GetRelatedMessagesAsync(Guid taskId, int take = 50) { IReadOnlyList<Conversation> conversations = await repository.GetConversationsAsync(); List<Message> result = new List<Message>(); foreach (Conversation conversation in conversations) { IReadOnlyList<Message> messages = await repository.GetMessagesAsync(conversation.Id, int.MaxValue); result.AddRange(messages.Where(x => x.RelatedTaskId == taskId)); } return result.OrderByDescending(x => x.CreatedAt).Take(take).OrderBy(x => x.CreatedAt).ToList(); }
        public async Task<Conversation> GetInitialConversationAsync()
        {
            IReadOnlyList<Conversation> all = await repository.GetConversationsAsync(); Guid? lastId = await repository.GetLastConversationIdAsync(); Conversation last = lastId.HasValue ? all.FirstOrDefault(x => x.Id == lastId.Value && !x.IsArchived) : null;
            if (last != null && last.CreatedAt.Date == clock.Now.Date) return last;
            Conversation today = all.Where(x => !x.IsArchived && x.CreatedAt.Date == clock.Now.Date).OrderByDescending(x => x.UpdatedAt).FirstOrDefault(); if (today != null) { await repository.SetLastConversationIdAsync(today.Id); return today; }
            return await CreateConversationAsync(false);
        }
        public async Task<Conversation> EnsureDailyConversationAsync(Conversation current) { if (current != null && current.CreatedAt.Date == clock.Now.Date) return current; return await GetInitialConversationAsync(); }
        public async Task<Conversation> CreateConversationAsync(bool manual)
        {
            DateTime now = clock.Now; string suffix = manual ? " 새 대화 " + now.ToString("HH:mm") : " 업무"; Conversation item = new Conversation { Id = Guid.NewGuid(), Title = now.ToString("yyyy-MM-dd") + suffix, CreatedAt = now, UpdatedAt = now, LastMessageAt = now, IsArchived = false }; await repository.CreateConversationAsync(item); await repository.SetLastConversationIdAsync(item.Id); RaiseChanged(); return item;
        }
        public async Task AddMessageAsync(Conversation conversation, Message message)
        {
            message.ConversationId = conversation.Id; await repository.AddMessageAsync(message); conversation.UpdatedAt = message.CreatedAt; conversation.LastMessageAt = message.CreatedAt; await repository.UpdateConversationAsync(conversation); await repository.SetLastConversationIdAsync(conversation.Id); RaiseChanged();
        }
        public async Task DeleteConversationAsync(Guid id) { await repository.DeleteConversationAsync(id); RaiseChanged(); }
        public async Task<IReadOnlyList<Conversation>> SearchAsync(string query)
        {
            IReadOnlyList<Conversation> conversations = await repository.GetConversationsAsync(); if (string.IsNullOrWhiteSpace(query)) return conversations.OrderByDescending(x => x.LastMessageAt).ToList(); string value = query.Trim(); List<Conversation> result = new List<Conversation>(); foreach (Conversation conversation in conversations) { if ((conversation.Title ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) { result.Add(conversation); continue; } IReadOnlyList<Message> messages = await repository.GetMessagesAsync(conversation.Id, int.MaxValue); if (messages.Any(x => (x.Content ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)) result.Add(conversation); } return result.OrderByDescending(x => x.LastMessageAt).ToList();
        }
        private void RaiseChanged() { EventHandler handler = ConversationsChanged; if (handler != null) handler(this, EventArgs.Empty); }
    }
}
