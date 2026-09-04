using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SlaveSplit.Models;

namespace SlaveSplit.Services
{
    public static class ChatTranscriptFormatter
    {
        public static string Format(Conversation conversation, IEnumerable<Message> messages)
        {
            StringBuilder text = new StringBuilder("[SlaveSplit Chat]");
            DateTime started = conversation == null ? DateTime.Now : conversation.CreatedAt;
            text.Append("\nSession: ").Append(started.ToString("yyyy-MM-dd HH:mm"));
            foreach (Message message in (messages ?? Enumerable.Empty<Message>()).OrderBy(x => x.CreatedAt))
            {
                text.Append("\n\n[").Append(message.CreatedAt.ToString("HH:mm")).Append("] ").Append(message.IsUser ? "User" : "Slave").Append("\n").Append(Redact(message.Content));
            }
            return text.ToString();
        }

        private static string Redact(string value)
        {
            string text = value ?? "";
            text = Regex.Replace(text, "(?i)(github[_ -]?token|api[_ -]?key|authorization)\\s*[:=]\\s*\\S+", "$1: [REDACTED]");
            return Regex.Replace(text, "(?i)\\b(gh[pousr]_[A-Za-z0-9]{20,}|sk-[A-Za-z0-9_-]{20,})\\b", "[REDACTED]");
        }
    }
}
