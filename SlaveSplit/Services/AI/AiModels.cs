using System;
using System.Collections.Generic;
using SlaveSplit.Models;
namespace SlaveSplit.Services.AI
{
    public enum AiProviderStatus { Unknown, Checking, Connected, Disconnected, Error }
    public enum AgentCommandAction { CreateTask, StartTask, CompleteTask, BlockTask, UnblockTask, AddMemo, CreateFollowup, UpdateTask, ShowToday, ShowRemaining, ShowCompletedToday, ShowYesterday, SearchTask, Unknown }
    public sealed class AiConversationMessage { public string Role { get; set; } public string Content { get; set; } }
    public sealed class AiTaskContext { public Guid Id { get; set; } public string Title { get; set; } public string Status { get; set; } }
    public sealed class AiRequest { public string SystemPrompt { get; set; } public string UserMessage { get; set; } public IList<AiConversationMessage> ConversationMessages { get; set; } public IList<AiTaskContext> TaskContext { get; set; } public DateTime CurrentDateTime { get; set; } }
    public sealed class AiResponse { public bool Success { get; set; } public string Content { get; set; } public string RawContent { get; set; } public string ErrorMessage { get; set; } public long ElapsedMilliseconds { get; set; } public string ProviderName { get; set; } public string ModelName { get; set; } }
    public sealed class AgentCommand
    {
        public AgentCommandAction Action { get; set; } public double Confidence { get; set; } public Guid? TaskId { get; set; } public string Title { get; set; } public string Description { get; set; } public string Status { get; set; } public string Priority { get; set; } public DateTime? DueDate { get; set; } public string BlockedReason { get; set; } public string Memo { get; set; }
    }
    public sealed class AgentCommandParseResult { public bool Success { get; set; } public AgentCommand Command { get; set; } public string ErrorMessage { get; set; } }
    public sealed class AgentCommandBatch { public IList<AgentCommand> Commands { get; set; } public string Summary { get; set; } public double Confidence { get; set; } public bool RequiresConfirmation { get; set; } public AgentCommandBatch() { Commands = new List<AgentCommand>(); } }
    public sealed class AgentCommandResult { public AgentCommandAction Action { get; set; } public bool Success { get; set; } public Guid? TaskId { get; set; } public string Message { get; set; } public string ErrorMessage { get; set; } }
    public sealed class AgentBatchResult { public int SuccessCount { get; set; } public int FailureCount { get; set; } public IList<AgentCommandResult> Results { get; set; } public AgentBatchResult() { Results = new List<AgentCommandResult>(); } }
    public sealed class PendingCommandContext { public AgentCommandAction Action { get; set; } public IList<Guid> CandidateTaskIds { get; set; } public string Memo { get; set; } public DateTime CreatedAt { get; set; } public PendingCommandContext() { CandidateTaskIds = new List<Guid>(); } }
    public sealed class AgentCommandBatchParseResult { public bool Success { get; set; } public AgentCommandBatch Batch { get; set; } public string ErrorMessage { get; set; } }
    public enum AiProviderKind { Mock, Company }
    public sealed class AiProviderOptions { public AiProviderKind Provider { get; set; } public string Endpoint { get; set; } public string Model { get; set; } public int TimeoutSeconds { get; set; } public double ConfidenceThreshold { get; set; } public int ConversationContextLimit { get; set; } public AiProviderOptions() { Provider = AiProviderKind.Mock; TimeoutSeconds = 30; ConfidenceThreshold = 0.80; ConversationContextLimit = 20; } }
}
