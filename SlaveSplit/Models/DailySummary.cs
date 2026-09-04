using System;
using System.Collections.Generic;
namespace SlaveSplit.Models
{
    public sealed class DailySummary
    {
        public DateTime Date { get; set; }
        public DateTime GeneratedAt { get; set; }
        public IList<WorkTask> Completed { get; set; }
        public IList<WorkTask> InProgress { get; set; }
        public IList<WorkTask> Remaining { get; set; }
        public IList<WorkTask> Blocked { get; set; }
        public IList<WorkTask> CarryOver { get; set; }
        public int CompletedCount { get { return Completed == null ? 0 : Completed.Count; } }
        public int RemainingCount { get { return Remaining == null ? 0 : Remaining.Count; } }
        public int BlockedCount { get { return Blocked == null ? 0 : Blocked.Count; } }
    }
}
