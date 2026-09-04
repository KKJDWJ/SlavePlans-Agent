using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
namespace SlaveSplit.Repositories
{
    public sealed class LocalTaskRepository : ITaskRepository
    {
        private readonly string tasksPath, historyPath; private readonly FileLogger logger; private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        public LocalTaskRepository(string directory = null)
        {
            string root = directory ?? Environment.GetEnvironmentVariable("SLAVESPLIT_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit");
            Directory.CreateDirectory(root); tasksPath = Path.Combine(root, "tasks.json"); historyPath = Path.Combine(root, "task_history.json"); logger = new FileLogger(root);
        }
        public async Task<IReadOnlyList<WorkTask>> GetAllAsync() { return await ReadAsync<WorkTask>(tasksPath, "Repository Load tasks 실패"); }
        public async Task<WorkTask> GetByIdAsync(Guid id) { return (await GetAllAsync()).FirstOrDefault(x => x.Id == id); }
        public async Task AddAsync(WorkTask task) { await MutateTasksAsync(list => { if (list.Any(x => x.Id == task.Id)) throw new InvalidOperationException("Duplicate task id."); list.Add(task); }); }
        public async Task UpdateAsync(WorkTask task) { await MutateTasksAsync(list => { int index = list.FindIndex(x => x.Id == task.Id); if (index < 0) throw new KeyNotFoundException("Task not found."); list[index] = task; }); }
        public async Task DeleteAsync(Guid id) { await MutateTasksAsync(list => list.RemoveAll(x => x.Id == id)); }
        public async Task<IReadOnlyList<WorkTask>> GetByStatusAsync(WorkTaskStatus status) { return (await GetAllAsync()).Where(x => x.Status == status).ToList(); }
        public async Task<IReadOnlyList<WorkTask>> GetCompletedByDateAsync(DateTime date) { return (await GetAllAsync()).Where(x => x.Status == WorkTaskStatus.Done && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == date.Date).ToList(); }
        public async Task<IReadOnlyList<WorkTask>> GetCreatedByDateAsync(DateTime date) { return (await GetAllAsync()).Where(x => x.CreatedAt.Date == date.Date).ToList(); }
        public async Task<IReadOnlyList<WorkTask>> GetByDueDateAsync(DateTime date) { return (await GetAllAsync()).Where(x => x.DueDate.HasValue && x.DueDate.Value.Date == date.Date).ToList(); }
        public async Task<IReadOnlyList<WorkTask>> QueryAsync(TaskQuery query)
        {
            IEnumerable<WorkTask> result = await GetAllAsync(); if (query == null) return result.ToList();
            if (query.StatusFilter == TaskStatusFilter.NotDone) result = result.Where(x => x.Status != WorkTaskStatus.Done); else if (query.Status.HasValue) result = result.Where(x => x.Status == query.Status.Value); if (query.Priority.HasValue) result = result.Where(x => x.Priority == query.Priority.Value); if (!string.IsNullOrWhiteSpace(query.Category)) result = result.Where(x => string.Equals(x.Category, query.Category, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(query.Keyword)) { string value = query.Keyword.Trim(); result = result.Where(x => (x.Title ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || (x.Description ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || (x.Category ?? "").IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0); }
            if (query.StartDate.HasValue || query.EndDate.HasValue) result = result.Where(x => { bool completed = query.DateField == TaskDateField.Completed || query.UseCompletedDate; DateTime? date = query.DateField == TaskDateField.Due ? (x.DueDate.HasValue ? (DateTime?)x.DueDate.Value.LocalDateTime : null) : completed ? (x.CompletedAt.HasValue ? (DateTime?)x.CompletedAt.Value.LocalDateTime : null) : query.DateField == TaskDateField.Created || query.UseCreatedDate ? (DateTime?)x.CreatedAt.LocalDateTime : (DateTime?)x.UpdatedAt; return (!completed || x.Status == WorkTaskStatus.Done) && date.HasValue && (!query.StartDate.HasValue || date.Value >= query.StartDate.Value) && (!query.EndDate.HasValue || date.Value < query.EndDate.Value); });
            return result.ToList();
        }
        public async Task AddHistoryAsync(TaskHistory history)
        {
            await gate.WaitAsync(); try { List<TaskHistory> list = ReadUnsafe<TaskHistory>(historyPath, "Repository Load history 실패", true); list.Add(history); WriteAtomic(historyPath, list); } catch (Exception ex) { logger.Error("Repository Save history 실패", ex); throw; } finally { gate.Release(); }
        }
        public async Task<IReadOnlyList<TaskHistory>> GetHistoryAsync(Guid? taskId = null) { IReadOnlyList<TaskHistory> all = await ReadAsync<TaskHistory>(historyPath, "Repository Load history 실패"); return taskId.HasValue ? all.Where(x => x.TaskId == taskId.Value).ToList() : all; }
        private async Task MutateTasksAsync(Action<List<WorkTask>> mutate)
        {
            await gate.WaitAsync(); try { List<WorkTask> list = ReadUnsafe<WorkTask>(tasksPath, "Repository Load tasks 실패", true); mutate(list); WriteAtomic(tasksPath, list); } catch (Exception ex) { logger.Error("Task Update/Save 실패", ex); throw; } finally { gate.Release(); }
        }
        private async Task<IReadOnlyList<T>> ReadAsync<T>(string path, string operation)
        {
            await gate.WaitAsync(); try { return ReadUnsafe<T>(path, operation); } finally { gate.Release(); }
        }
        private List<T> ReadUnsafe<T>(string path, string operation, bool throwOnFailure = false)
        {
            if (!File.Exists(path)) return new List<T>();
            try { using (FileStream stream = File.OpenRead(path)) return (List<T>)new DataContractJsonSerializer(typeof(List<T>)).ReadObject(stream); }
            catch (Exception ex) { logger.Error(operation, ex); if (throwOnFailure) throw; return new List<T>(); }
        }
        private void WriteAtomic<T>(string path, List<T> data)
        {
            string temp = path + ".tmp"; string backup = path + ".bak";
            try
            {
                using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { new DataContractJsonSerializer(typeof(List<T>)).WriteObject(stream, data); stream.Flush(true); }
                if (File.Exists(path)) File.Replace(temp, path, backup, true); else File.Move(temp, path);
            }
            catch (Exception ex) { logger.Error("Serialization/Save 실패", ex); try { if (File.Exists(temp)) File.Delete(temp); } catch { } throw; }
        }
    }
}
