using System;
using System.Runtime.Serialization;
using SlaveSplit.Services.GitHub;
namespace SlaveSplit.Models
{
    public enum WorkTaskStatus { Todo, InProgress, Blocked, Done }
    public enum TaskPriority { Low, Normal, High, Critical }
    public enum TaskSource { Local, GitHub }
    [DataContract]
    public sealed class WorkTask
    {
        [DataMember] public Guid Id { get; set; }
        [DataMember] public string Title { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public WorkTaskStatus Status { get; set; }
        [IgnoreDataMember] public TaskPriority Priority { get; set; } = TaskPriority.Normal;
        [DataMember(Name = "Priority", EmitDefaultValue = false)] private TaskPriority? PriorityStorage { get { return Priority; } set { Priority = value ?? TaskPriority.Normal; } }
        [DataMember] public string Category { get; set; }
        [DataMember] public string WorkspaceId { get; set; }
        [IgnoreDataMember] public DateTimeOffset CreatedAt { get; set; }
        [DataMember] public DateTime UpdatedAt { get; set; }
        [DataMember] public DateTime? StartedAt { get; set; }
        [IgnoreDataMember] public DateTimeOffset? CompletedAt { get; set; }
        [IgnoreDataMember] public DateTimeOffset? DueDate { get; set; }
        [DataMember] public TimeSpan? DueTime { get; set; }
        [DataMember] public int? ReminderOffsetMinutes { get; set; }
        [DataMember] public bool ReminderEnabled { get; set; }
        [IgnoreDataMember] public DateTimeOffset? LastReminderNotifiedAt { get; set; }
        [DataMember(Name = "CreatedAt")] private DateTime CreatedAtStorage { get { return CreatedAt.LocalDateTime; } set { CreatedAt = LocalOffset(value); } }
        [DataMember(Name = "CompletedAt")] private DateTime? CompletedAtStorage { get { return CompletedAt.HasValue ? (DateTime?)CompletedAt.Value.LocalDateTime : null; } set { CompletedAt = value.HasValue ? (DateTimeOffset?)LocalOffset(value.Value) : null; } }
        [DataMember(Name = "DueDate")] private DateTime? DueDateStorage { get { return DueDate.HasValue ? (DateTime?)DueDate.Value.LocalDateTime : null; } set { DueDate = value.HasValue ? (DateTimeOffset?)LocalOffset(value.Value) : null; } }
        [DataMember(Name = "LastReminderNotifiedAt")] private DateTime? LastReminderNotifiedAtStorage { get { return LastReminderNotifiedAt.HasValue ? (DateTime?)LastReminderNotifiedAt.Value.LocalDateTime : null; } set { LastReminderNotifiedAt = value.HasValue ? (DateTimeOffset?)LocalOffset(value.Value) : null; } }
        [DataMember] public string BlockedReason { get; set; }
        [DataMember] public Guid? ParentTaskId { get; set; }
        [DataMember] public TaskSource Source { get; set; }
        [DataMember] public string ExternalId { get; set; }
        [DataMember] public string ExternalUrl { get; set; }
        [DataMember] public string GitHubNodeId { get; set; }
        [DataMember] public string GitHubProjectItemId { get; set; }
        [DataMember] public GitHubSyncStatus GitHubProjectSyncStatus { get; set; }
        [DataMember] public GitHubSyncStatus GitHubSyncStatus { get; set; }
        [DataMember] public DateTime? LastGitHubSyncAt { get; set; }
        [DataMember] public string LastGitHubSyncError { get; set; }
        public string StatusText { get { return Status == WorkTaskStatus.InProgress ? "IN PROGRESS" : Status.ToString().ToUpperInvariant(); } }
        public string Metadata { get { return string.IsNullOrWhiteSpace(Category) ? PriorityDisplayText : Category + " · " + PriorityDisplayText; } }
        public string TimeText { get { if (Status == WorkTaskStatus.Done && CompletedAt.HasValue) return "Completed " + CompletedAt.Value.ToString("HH:mm"); if (Status == WorkTaskStatus.InProgress && StartedAt.HasValue) return "Started " + StartedAt.Value.ToString("HH:mm"); if (DueDate.HasValue) return "Due " + DueDate.Value.ToString("MMM d"); return "Created " + CreatedAt.ToString("HH:mm"); } }
        public bool IsOverdue { get { return Status != WorkTaskStatus.Done && DueDate.HasValue && DueDate.Value.Date < DateTime.Today; } }
        public string DueLabel { get { if (!DueDate.HasValue) return ""; string label=IsOverdue?"OVERDUE · "+DueDate.Value.ToString("MMM d"):DueDate.Value.Date==DateTime.Today?"TODAY":DueDate.Value.Date==DateTime.Today.AddDays(1)?"Tomorrow":DueDate.Value.ToString("MMM d");return label+(DueTime.HasValue?" "+DueTime.Value.ToString(@"hh\:mm")+(ReminderEnabled?" · 알림":""):""); } }
        public string PriorityText { get { return Priority == TaskPriority.Critical ? "긴급" : Priority == TaskPriority.High ? "높음" : Priority == TaskPriority.Low ? "낮음" : ""; } }
        public string PriorityDisplayText { get { return Priority == TaskPriority.Critical ? "긴급" : Priority == TaskPriority.High ? "높음" : Priority == TaskPriority.Low ? "낮음" : "보통"; } }
        public DateTimeOffset? DueDateTime { get { return DueDate.HasValue&&DueTime.HasValue?(DateTimeOffset?)new DateTimeOffset(DueDate.Value.Date.Add(DueTime.Value)):null; } }
        public DateTimeOffset? ReminderAt { get { return ReminderEnabled&&DueDateTime.HasValue?(DateTimeOffset?)DueDateTime.Value.AddMinutes(-(ReminderOffsetMinutes??10)):null; } }
        public string DueDateTimeLabel { get { return DueLabel; } }
        public bool HasPriorityBadge { get { return Priority != TaskPriority.Normal; } }
        private static DateTimeOffset LocalOffset(DateTime value) { DateTime local = DateTime.SpecifyKind(value, DateTimeKind.Local); return new DateTimeOffset(local); }
    }
}
