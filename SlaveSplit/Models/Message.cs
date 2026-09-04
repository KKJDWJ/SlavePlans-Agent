using System;
namespace SlaveSplit.Models
{
    public enum MessageType { User, Assistant, System }
    public sealed class Message
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public string Sender { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public MessageType MessageType { get; set; }
        public Guid? RelatedTaskId { get; set; }
        public bool IsUser { get { return MessageType == MessageType.User; } }
        public string TimeText { get { return CreatedAt.ToString("HH:mm"); } }
    }
}
