using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Models;
using SlaveSplit.Repositories;

namespace SlaveSplit.AgentTests
{
    public sealed class InMemoryTaskRepository : ITaskRepository
    {
        private readonly List<WorkTask> tasks = new List<WorkTask>(); private readonly List<TaskHistory> history = new List<TaskHistory>(); public bool FailUpdates { get; set; }
        public Task<IReadOnlyList<WorkTask>> GetAllAsync() { return Task.FromResult((IReadOnlyList<WorkTask>)tasks.ToList()); }
        public Task<WorkTask> GetByIdAsync(Guid id) { return Task.FromResult(tasks.FirstOrDefault(x => x.Id == id)); }
        public Task AddAsync(WorkTask task) { if (task == null) throw new ArgumentNullException("task"); tasks.Add(task); return Task.FromResult(0); }
        public Task UpdateAsync(WorkTask task) { if(FailUpdates)throw new InvalidOperationException("Simulated update failure."); int i = tasks.FindIndex(x => x.Id == task.Id); if (i < 0) throw new KeyNotFoundException(); tasks[i] = task; return Task.FromResult(0); }
        public Task DeleteAsync(Guid id) { tasks.RemoveAll(x => x.Id == id); return Task.FromResult(0); }
        public Task<IReadOnlyList<WorkTask>> GetByStatusAsync(WorkTaskStatus status) { return Result(tasks.Where(x => x.Status == status)); }
        public Task<IReadOnlyList<WorkTask>> GetCompletedByDateAsync(DateTime date) { return Result(tasks.Where(x => x.Status == WorkTaskStatus.Done && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == date.Date)); }
        public Task<IReadOnlyList<WorkTask>> GetCreatedByDateAsync(DateTime date) { return Result(tasks.Where(x => x.CreatedAt.Date == date.Date)); }
        public Task<IReadOnlyList<WorkTask>> GetByDueDateAsync(DateTime date) { return Result(tasks.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date == date.Date)); }
        public Task<IReadOnlyList<WorkTask>> QueryAsync(TaskQuery q) { IEnumerable<WorkTask> r = tasks; if (q == null) return Result(r); if (q.StatusFilter == TaskStatusFilter.NotDone) r = r.Where(x => x.Status != WorkTaskStatus.Done); else if (q.Status.HasValue) r = r.Where(x => x.Status == q.Status); if (!string.IsNullOrWhiteSpace(q.Keyword)) r = r.Where(x => (x.Title ?? "").IndexOf(q.Keyword, StringComparison.OrdinalIgnoreCase) >= 0 || (x.Description ?? "").IndexOf(q.Keyword, StringComparison.OrdinalIgnoreCase) >= 0); if (q.StartDate.HasValue || q.EndDate.HasValue) r = r.Where(x => { bool completed = q.DateField == TaskDateField.Completed || q.UseCompletedDate; DateTime? d = q.DateField == TaskDateField.Due ? (x.DueDate.HasValue ? (DateTime?)x.DueDate.Value.LocalDateTime : null) : completed ? (x.CompletedAt.HasValue ? (DateTime?)x.CompletedAt.Value.LocalDateTime : null) : q.DateField == TaskDateField.Created || q.UseCreatedDate ? (DateTime?)x.CreatedAt.LocalDateTime : (DateTime?)x.UpdatedAt; return (!completed || x.Status == WorkTaskStatus.Done) && d.HasValue && (!q.StartDate.HasValue || d >= q.StartDate) && (!q.EndDate.HasValue || d < q.EndDate); }); return Result(r); }
        public Task AddHistoryAsync(TaskHistory item) { history.Add(item); return Task.FromResult(0); }
        public Task<IReadOnlyList<TaskHistory>> GetHistoryAsync(Guid? taskId = null) { return Task.FromResult((IReadOnlyList<TaskHistory>)history.Where(x => !taskId.HasValue || x.TaskId == taskId).ToList()); }
        private static Task<IReadOnlyList<WorkTask>> Result(IEnumerable<WorkTask> value) { return Task.FromResult((IReadOnlyList<WorkTask>)value.ToList()); }
    }
    public sealed class AgentTestClock : SlaveSplit.Infrastructure.IClock { public DateTime Now { get; set; } = new DateTime(2026, 9, 1, 14, 0, 0); }
}
