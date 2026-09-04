using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Repositories;
using SlaveSplit.Services.GitHub;
namespace SlaveSplit.Services
{
    public sealed class TaskService
    {
        public event EventHandler TasksChanged;
        private readonly ITaskRepository repository; private readonly IClock clock; public IGitHubIssueCreationService GitHubCreationService { get; set; } public IGitHubSyncService GitHubSyncService { get; set; } public GitHubTaskCreationResult LastGitHubCreationResult { get; private set; }
        public TaskService(ITaskRepository repository, IClock clock = null) { this.repository = repository; this.clock = clock ?? new SystemClock(); }
        public Task<IReadOnlyList<WorkTask>> GetAllAsync() { return repository.GetAllAsync(); }
        public Task<WorkTask> GetByIdAsync(Guid id) { return repository.GetByIdAsync(id); }
        public async Task DeleteAsync(Guid id) { WorkTask task = await repository.GetByIdAsync(id); if (task == null) return; await repository.DeleteAsync(id); RaiseChanged(); }
        public Task<IReadOnlyList<TaskHistory>> GetHistoryAsync(Guid? taskId = null) { return repository.GetHistoryAsync(taskId); }
        public Task<IReadOnlyList<WorkTask>> QueryAsync(TaskQuery query) { return repository.QueryAsync(query); }
        public async Task SaveIntegrationStateAsync(WorkTask task) { if (task == null) return; await repository.UpdateAsync(task); RaiseChanged(); }
        public Task RecordGitHubSyncAsync(WorkTask task, string memo) { return task == null ? Task.FromResult(0) : RecordAsync(task, TaskHistoryAction.GitHubSynced, task.Status, task.Status, memo, null); }
        public async Task<WorkTask> CreateTaskAsync(string title, string description = null, string category = null, TaskPriority priority = TaskPriority.Normal, DateTime? dueDate = null, bool allowGitHubAutoCreate = true)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required."); DateTime now = clock.Now;
            WorkTask task = new WorkTask { Id = Guid.NewGuid(), Title = title.Trim(), Description = description ?? "", Category = string.IsNullOrWhiteSpace(category) ? "기타" : category.Trim(), Priority = priority, Status = WorkTaskStatus.Todo, Source = TaskSource.Local, CreatedAt = now, UpdatedAt = now, DueDate = dueDate };
            await repository.AddAsync(task); await RecordAsync(task, TaskHistoryAction.Created, null, WorkTaskStatus.Todo, "업무 등록", null); RaiseChanged(); LastGitHubCreationResult = null; if (allowGitHubAutoCreate && GitHubCreationService != null && GitHubCreationService.IsAutoCreateEnabled) LastGitHubCreationResult = await GitHubCreationService.CreateForTaskAsync(task, System.Threading.CancellationToken.None); return task;
        }
        public Task StartAsync(WorkTask task, string memo = null) { return ChangeStatusAsync(task, WorkTaskStatus.InProgress, memo ?? "업무 시작", null); }
        public Task CompleteAsync(WorkTask task, string memo = null) { return ChangeStatusAsync(task, WorkTaskStatus.Done, memo ?? "업무 완료", null); }
        public Task BlockAsync(WorkTask task, string reason) { if (string.IsNullOrWhiteSpace(reason)) reason = "외부 조건 확인 필요"; return ChangeStatusAsync(task, WorkTaskStatus.Blocked, reason, reason); }
        public Task MoveToTodoAsync(WorkTask task, string memo = null) { return ChangeStatusAsync(task, WorkTaskStatus.Todo, memo ?? "Todo로 이동", null); }
        public Task UpdateAsync(WorkTask task) { return UpdateAsync(task, "업무 수정"); }
        public async Task<bool> UpdateDueDateAsync(WorkTask task, DateTimeOffset? dueDate) { if (task == null) throw new ArgumentNullException("task"); if ((!task.DueDate.HasValue && !dueDate.HasValue) || (task.DueDate.HasValue && dueDate.HasValue && task.DueDate.Value.Date == dueDate.Value.Date)) return false; task.DueDate = dueDate; await UpdateAsync(task, dueDate.HasValue ? "업무 일정 변경" : "업무 일정 제거"); return true; }
        public async Task<bool> UpdatePriorityAsync(WorkTask task, TaskPriority priority) { if (task == null) throw new ArgumentNullException("task"); if (task.Priority == priority) return false; task.Priority = priority; await UpdateAsync(task, "업무 우선순위 변경"); return true; }
        public async Task<bool> UpdateTimeAsync(WorkTask task,TimeSpan? dueTime,int? reminderOffset=null){if(task==null)throw new ArgumentNullException("task");int? offset=dueTime.HasValue?(reminderOffset??task.ReminderOffsetMinutes??10):(int?)null;bool enabled=dueTime.HasValue;if(task.DueTime==dueTime&&task.ReminderEnabled==enabled&&task.ReminderOffsetMinutes==offset)return false;task.DueTime=dueTime;task.ReminderEnabled=enabled;task.ReminderOffsetMinutes=offset;task.LastReminderNotifiedAt=null;await UpdateAsync(task,dueTime.HasValue?"업무 시간 변경":"업무 시간 제거");return true;}
        public async Task<bool> SetReminderEnabledAsync(WorkTask task,bool enabled){if(task==null)throw new ArgumentNullException("task");if(enabled&&!task.DueTime.HasValue)return false;if(task.ReminderEnabled==enabled)return false;task.ReminderEnabled=enabled;if(enabled&&!task.ReminderOffsetMinutes.HasValue)task.ReminderOffsetMinutes=10;task.LastReminderNotifiedAt=null;await UpdateAsync(task,enabled?"알림 활성화":"알림 비활성화");return true;}
        public async Task UpdateDescriptionAsync(WorkTask task, string description) { if (task == null) throw new ArgumentNullException("task"); task.Description = description ?? ""; await UpdateAsync(task, "업무 설명 수정"); await TryGitHubSyncAsync(delegate { return GitHubSyncService.QueueIssueBodyAsync(task, task.Description); }); }
        public async Task UpdateAsync(WorkTask task, string memo) { task.UpdatedAt = clock.Now; await repository.UpdateAsync(task); await RecordAsync(task, TaskHistoryAction.Updated, task.Status, task.Status, string.IsNullOrWhiteSpace(memo) ? "업무 수정" : memo, null); RaiseChanged(); }
        public async Task AddMemoAsync(WorkTask task, string memo) { if (task == null) throw new ArgumentNullException("task"); if (string.IsNullOrWhiteSpace(memo)) throw new ArgumentException("Memo is required."); task.UpdatedAt = clock.Now; await repository.UpdateAsync(task); await RecordAsync(task, TaskHistoryAction.MemoAdded, task.Status, task.Status, memo.Trim(), null); RaiseChanged(); await TryGitHubSyncAsync(delegate { return GitHubSyncService.QueueCommentAsync(task, memo.Trim()); }); }
        public async Task<WorkTask> CreateFollowUpAsync(WorkTask parent, string title, string description = null, TaskPriority priority = TaskPriority.Normal, DateTime? dueDate = null)
        {
            if (parent == null) throw new ArgumentNullException("parent"); WorkTask child = await CreateTaskAsync(title, description, parent.Category, priority, dueDate); child.ParentTaskId = parent.Id; child.UpdatedAt = clock.Now; await repository.UpdateAsync(child); await RecordAsync(parent, TaskHistoryAction.FollowUpCreated, parent.Status, parent.Status, child.Title, child.Id); await RecordAsync(child, TaskHistoryAction.CreatedFromTask, child.Status, child.Status, parent.Title, parent.Id); RaiseChanged(); return child;
        }
        public async Task UnblockAsync(WorkTask task, bool start, string memo = null) { if (task.Status != WorkTaskStatus.Blocked) throw new InvalidOperationException("Only blocked tasks can be unblocked."); WorkTaskStatus previous = task.Status; DateTime now = clock.Now; task.Status = start ? WorkTaskStatus.InProgress : WorkTaskStatus.Todo; if (start && !task.StartedAt.HasValue) task.StartedAt = now; task.BlockedReason = null; task.UpdatedAt = now; await repository.UpdateAsync(task); await RecordAsync(task, TaskHistoryAction.Unblocked, previous, task.Status, memo ?? "Blocked 해제", null); RaiseChanged(); }
        private async Task ChangeStatusAsync(WorkTask task, WorkTaskStatus next, string memo, string blockReason)
        {
            WorkTaskStatus previous = task.Status; if (previous == next) return; DateTime now = clock.Now; task.Status = next; task.UpdatedAt = now;
            if (next == WorkTaskStatus.InProgress && !task.StartedAt.HasValue) task.StartedAt = now;
            if (next == WorkTaskStatus.Done && previous != WorkTaskStatus.Done) task.CompletedAt = now; else if (next != WorkTaskStatus.Done) task.CompletedAt = null;
            task.BlockedReason = next == WorkTaskStatus.Blocked ? blockReason : null;
            await repository.UpdateAsync(task); TaskHistoryAction action = next == WorkTaskStatus.InProgress ? TaskHistoryAction.Started : next == WorkTaskStatus.Done ? TaskHistoryAction.Completed : next == WorkTaskStatus.Blocked ? TaskHistoryAction.Blocked : previous == WorkTaskStatus.Blocked ? TaskHistoryAction.Unblocked : TaskHistoryAction.Updated; await RecordAsync(task, action, previous, next, memo, null); RaiseChanged(); if (next == WorkTaskStatus.Done) await TryGitHubSyncAsync(delegate { return GitHubSyncService.QueueCloseAsync(task); }); else if (previous == WorkTaskStatus.Done && next == WorkTaskStatus.Todo) await TryGitHubSyncAsync(delegate { return GitHubSyncService.QueueReopenAsync(task); }); await TryGitHubSyncAsync(delegate { return GitHubSyncService.QueueProjectStatusAsync(task); });
        }
        private async Task TryGitHubSyncAsync(Func<Task> operation) { if (GitHubSyncService == null || operation == null) return; try { await operation(); } catch { } }
        private Task RecordAsync(WorkTask task, TaskHistoryAction action, WorkTaskStatus? previous, WorkTaskStatus? next, string memo, Guid? relatedTaskId) { return repository.AddHistoryAsync(new TaskHistory { Id = Guid.NewGuid(), TaskId = task.Id, Action = action.ToString(), ActionType = action, PreviousStatus = previous, NewStatus = next, Memo = memo, CreatedAt = clock.Now, RelatedTaskId = relatedTaskId }); }
        private void RaiseChanged() { EventHandler handler = TasksChanged; if (handler != null) handler(this, EventArgs.Empty); }
    }
}
