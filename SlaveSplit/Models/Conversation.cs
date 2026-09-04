using System;
namespace SlaveSplit.Models
{
    public sealed class Conversation
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime LastMessageAt { get; set; }
        public bool IsArchived { get; set; }
    }
}
