using System;
using System.Runtime.Serialization;
namespace SlaveSplit.Models
{
    public enum TaskHistoryAction { Created, Started, Completed, Blocked, Unblocked, MemoAdded, Updated, FollowUpCreated, CreatedFromTask, GitHubSynced }
    [DataContract]
    public sealed class TaskHistory
    {
        [DataMember] public Guid Id { get; set; }
        [DataMember] public Guid TaskId { get; set; }
        [DataMember] public string Action { get; set; }
        [DataMember] public TaskHistoryAction ActionType { get; set; }
        [DataMember] public WorkTaskStatus? PreviousStatus { get; set; }
        [DataMember] public WorkTaskStatus? NewStatus { get; set; }
        [DataMember] public string Memo { get; set; }
        [DataMember] public DateTime CreatedAt { get; set; }
        [DataMember] public Guid? RelatedTaskId { get; set; }
    }
}
