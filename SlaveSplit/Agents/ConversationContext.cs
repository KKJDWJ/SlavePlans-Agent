using System;
using System.Collections.Generic;
using System.Linq;
using SlaveSplit.Models;
namespace SlaveSplit.Agents
{
    public enum PendingAgentActionType { None, DeleteTasks, AddMemo, CreateTask, UpdateDescription, ChangeStatus, ImportGitHubIssue }
    public enum PendingAgentNeed { None, Target, Content, Confirmation }
    public sealed class PendingAgentAction
    {
        public PendingAgentAction() { TargetTaskIds = new List<Guid>(); Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
        public PendingAgentActionType ActionType { get; set; } public PendingAgentNeed Need { get; set; } public IList<Guid> TargetTaskIds { get; set; } public int? GitHubIssueNumber { get; set; } public IDictionary<string, string> Arguments { get; set; } public DateTime CreatedAt { get; set; }
    }
    public sealed class ConversationContext
    {
        private readonly List<DisplayedTaskContext> displayedTasks = new List<DisplayedTaskContext>();
        public IReadOnlyList<DisplayedTaskContext> DisplayedTasks { get { return displayedTasks.AsReadOnly(); } }
        public Guid? LastReferencedTaskId { get; private set; }
        public Guid? LastCreatedTaskId { get; private set; }
        public Guid? LastModifiedTaskId { get; private set; }
        public Guid? LastCompletedTaskId { get; private set; }
        public Guid? LastDeletedTaskId { get; private set; }
        public string LastCreatedTaskTitle { get; private set; }
        public int? LastReferencedGitHubIssueNumber { get; private set; }
        public int? LastCreatedGitHubIssueNumber { get; private set; }
        public IReadOnlyList<Guid> LastReadTaskIds { get; private set; }
        public string LastToolCall { get; private set; }
        public string LastToolResult { get; private set; }
        public string LastChangedField { get; private set; }
        public PendingAgentAction PendingAction { get; private set; }
        public string LastResponseType { get; private set; }
        public int LastTodoCount { get; private set; }
        public ConversationContext() { LastReadTaskIds = new List<Guid>().AsReadOnly(); }
        public void SetDisplayedTasks(IEnumerable<WorkTask> tasks, DateTime displayedAt)
        {
            displayedTasks.Clear(); int number = 1;
            foreach (WorkTask task in tasks) displayedTasks.Add(new DisplayedTaskContext { DisplayNumber = number++, TaskId = task.Id, DisplayedAt = displayedAt });
        }
        public Guid? Resolve(int number) { DisplayedTaskContext item = displayedTasks.FirstOrDefault(x => x.DisplayNumber == number); return item == null ? (Guid?)null : item.TaskId; }
        public IReadOnlyList<Guid> ResolveMany(IEnumerable<int> numbers) { return (numbers ?? Enumerable.Empty<int>()).Select(Resolve).Where(x => x.HasValue).Select(x => x.Value).Distinct().ToList().AsReadOnly(); }
        public IReadOnlyList<Guid> ResolveAll() { return displayedTasks.OrderBy(x => x.DisplayNumber).Select(x => x.TaskId).ToList().AsReadOnly(); }
        public Guid? ResolveLast() { DisplayedTaskContext item = displayedTasks.OrderByDescending(x => x.DisplayNumber).FirstOrDefault(); return item == null ? (Guid?)null : item.TaskId; }
        public void Reference(Guid? taskId) { if (taskId.HasValue) LastReferencedTaskId = taskId; }
        public void RecordTool(string name, string result, Guid? taskId) { LastToolCall = name; LastToolResult = result; Reference(taskId); if (!taskId.HasValue || !string.Equals(result, "success", StringComparison.OrdinalIgnoreCase)) return; if (name == "create_task") LastCreatedTaskId = taskId; if (name == "complete_task") LastCompletedTaskId = taskId; if (name == "start_task" || name == "complete_task" || name == "reopen_task" || name == "block_task" || name == "add_task_memo" || name == "update_task_description" || name == "update_task_due_date" || name == "update_task_priority" || name == "update_task_time" || name == "update_task_reminder") LastModifiedTaskId = taskId; if(name=="update_task_time")LastChangedField="DueTime";else if(name=="update_task_due_date")LastChangedField="DueDate";else if(name=="update_task_priority")LastChangedField="Priority";else if(name=="update_task_description")LastChangedField="Description";else if(name=="update_task_reminder")LastChangedField="Reminder";else if(name=="start_task"||name=="complete_task"||name=="reopen_task"||name=="block_task")LastChangedField="Status"; }
        public void RecordCreated(Guid id, string title) { LastCreatedTaskId = id; LastCreatedTaskTitle = title; LastReferencedTaskId = id; }
        public void RecordTaskSummary(int todoCount) { LastResponseType = "TASK_SUMMARY"; LastTodoCount = todoCount; }
        public void RecordRead(IEnumerable<Guid> ids) { List<Guid> values = (ids ?? Enumerable.Empty<Guid>()).Distinct().ToList(); LastReadTaskIds = values.AsReadOnly(); if (values.Count == 1) Reference(values[0]); }
        public void ReferenceGitHub(int? number, bool created) { if (!number.HasValue) return; LastReferencedGitHubIssueNumber = number; if (created) LastCreatedGitHubIssueNumber = number; }
        public void SetPending(PendingAgentAction value) { PendingAction = value; }
        public PendingAgentAction TakePending() { PendingAgentAction value = PendingAction; PendingAction = null; return value; }
        public void ClearPending() { PendingAction = null; }
        public void RecordDeleted(Guid taskId) { LastDeletedTaskId = taskId; RemoveTask(taskId); }
        public void RemoveTask(Guid taskId) { displayedTasks.RemoveAll(x => x.TaskId == taskId); if (LastReferencedTaskId == taskId) LastReferencedTaskId = null; if (LastCreatedTaskId == taskId) LastCreatedTaskId = null; if (LastModifiedTaskId == taskId) LastModifiedTaskId = null; if (LastCompletedTaskId == taskId) LastCompletedTaskId = null; LastReadTaskIds = LastReadTaskIds.Where(x => x != taskId).ToList().AsReadOnly(); }
        public void Clear() { displayedTasks.Clear(); LastReferencedTaskId = null; LastCreatedTaskId = null; LastModifiedTaskId = null; LastCompletedTaskId = null; LastDeletedTaskId = null; LastCreatedTaskTitle = null; LastReferencedGitHubIssueNumber = null; LastCreatedGitHubIssueNumber = null; LastReadTaskIds = new List<Guid>().AsReadOnly(); LastToolCall = null; LastToolResult = null; LastResponseType = null; LastTodoCount = 0; PendingAction = null; }
    }
}
