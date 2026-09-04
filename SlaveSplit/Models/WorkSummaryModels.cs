using System;
using System.Collections.Generic;
namespace SlaveSplit.Models
{
    public enum TaskDateField { Updated, Created, Due, Completed }
    public enum TaskStatusFilter { All, Exact, NotDone }
    public enum TaskDateRangeKind { Today, Tomorrow, Yesterday, ThisWeek, SpecificDate }
    public sealed class DateFilter { public TaskDateField Type { get; set; } public TaskDateRangeKind Range { get; set; } public DateTime? Date { get; set; } public DateTime Start { get; set; } public DateTime EndExclusive { get; set; } }
    public sealed class DateRange
    {
        public DateTime StartDate { get; set; } public DateTime EndDate { get; set; }
        public bool Contains(DateTime value) { return value.Date >= StartDate.Date && value.Date <= EndDate.Date; }
    }
    public sealed class TaskQuery
    {
        public DateTime? StartDate { get; set; } public DateTime? EndDate { get; set; } public WorkTaskStatus? Status { get; set; } public TaskStatusFilter StatusFilter { get; set; } public string Keyword { get; set; } public string Category { get; set; } public TaskPriority? Priority { get; set; } public bool UseCompletedDate { get; set; } public bool UseCreatedDate { get; set; } public TaskDateField DateField { get; set; }
    }
    public sealed class DailyTaskGroup
    {
        public DateTime Date { get; set; } public IList<WorkTask> Tasks { get; set; } public DailyTaskGroup() { Tasks = new List<WorkTask>(); }
        public string Header { get { return Date.ToString("ddd · MMM d").ToUpperInvariant(); } }
    }
    public sealed class WeeklySummary
    {
        public DateRange Range { get; set; } public IList<WorkTask> CompletedTasks { get; set; } public IList<WorkTask> CreatedTasks { get; set; } public IList<WorkTask> RemainingTasks { get; set; } public IList<WorkTask> BlockedTasks { get; set; } public IList<WorkTask> InProgressTasks { get; set; } public IList<WorkTask> CarryOverTasks { get; set; } public IList<DailyTaskGroup> DailySummaries { get; set; }
        public DateTime StartDate { get { return Range.StartDate; } } public DateTime EndDate { get { return Range.EndDate; } } public int CompletedCount { get { return CompletedTasks.Count; } } public int CreatedCount { get { return CreatedTasks.Count; } } public int RemainingCount { get { return RemainingTasks.Count; } } public int BlockedCount { get { return BlockedTasks.Count; } } public int InProgressCount { get { return InProgressTasks.Count; } } public int CarryOverCount { get { return CarryOverTasks.Count; } } public int TotalCount { get { return CompletedCount + RemainingCount; } } public double CompletionRate { get { return TotalCount == 0 ? 0 : (double)CompletedCount / TotalCount; } }
    }
    public enum ReportFormat { Detailed, Compact }
}
