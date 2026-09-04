using System;
namespace SlaveSplit.Models
{
    public enum AgentIntent { Unknown, CreateTask, StartTask, CompleteTask, BlockTask, ShowToday, ShowRemaining, ShowCompletedToday, ShowYesterday, ShowTask, ShowThisWeek, ShowLastWeek, ShowDate, ShowDateRange, ShowWeekRemaining }
    public sealed class AgentRequest { public string Message { get; set; } public DateTime CreatedAt { get; set; } public System.Collections.Generic.IList<Message> ConversationMessages { get; set; } }
    public sealed class AgentResponse
    {
        public string Message { get; set; }
        public AgentIntent Intent { get; set; }
        public bool Success { get; set; }
        public Guid? RelatedTaskId { get; set; }
        public string ErrorMessage { get; set; }
        public bool ClearConversation { get; set; }
        public string InterpreterSource { get; set; }
        public bool FallbackCalled { get; set; }
        public string FallbackReason { get; set; }
        public bool LlmCalled { get; set; }
        public bool LlmSuccess { get; set; }
        public string LlmFailureKind { get; set; }
        public string LlmRawOutput { get; set; }
    }
    public sealed class DisplayedTaskContext { public int DisplayNumber { get; set; } public Guid TaskId { get; set; } public DateTime DisplayedAt { get; set; } }
}
