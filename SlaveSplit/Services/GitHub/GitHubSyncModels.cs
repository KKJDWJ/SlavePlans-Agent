using System;
using System.Net;
using System.Runtime.Serialization;
namespace SlaveSplit.Services.GitHub
{
    public enum GitHubSyncStatus { NotRequired, Synced, Pending, Syncing, Failed }
    public enum GitHubOperationType { CloseIssue, ReopenIssue, AddComment, UpdateIssueBody, AddProjectItem, UpdateProjectStatus }
    [DataContract] public sealed class PendingGitHubOperation
    {
        [DataMember] public Guid Id { get; set; } [DataMember] public Guid TaskId { get; set; } [DataMember] public int IssueNumber { get; set; } [DataMember] public GitHubOperationType OperationType { get; set; } [DataMember] public string Payload { get; set; } [DataMember] public DateTime CreatedAt { get; set; } [DataMember] public int RetryCount { get; set; } [DataMember] public DateTime? LastAttemptAt { get; set; } [DataMember] public string LastError { get; set; }
    }
    public sealed class GitHubOperationResult { public bool Success { get; set; } public HttpStatusCode? StatusCode { get; set; } public string Message { get; set; } public long ElapsedMilliseconds { get; set; } }
}
