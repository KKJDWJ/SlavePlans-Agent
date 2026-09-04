using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
namespace SlaveSplit.Services
{
    public sealed class DailySummaryService
    {
        private readonly TaskService tasks; private readonly IClock clock;
        public DailySummaryService(TaskService tasks, IClock clock = null) { this.tasks = tasks; this.clock = clock ?? new SystemClock(); }
        public async Task<DailySummary> GenerateAsync(DateTime date)
        {
            DateTime day = date.Date; IReadOnlyList<WorkTask> all = await tasks.GetAllAsync();
            return new DailySummary
            {
                Date = day, GeneratedAt = clock.Now,
                Completed = all.Where(x => x.CompletedAt.HasValue && x.CompletedAt.Value.Date == day).OrderBy(x => x.CompletedAt).ToList(),
                InProgress = all.Where(x => x.Status == WorkTaskStatus.InProgress).OrderByDescending(x => x.UpdatedAt).ToList(),
                Remaining = all.Where(x => x.Status != WorkTaskStatus.Done).OrderBy(x => x.DueDate ?? DateTime.MaxValue).ThenByDescending(x => x.Priority).ToList(),
                Blocked = all.Where(x => x.Status == WorkTaskStatus.Blocked).OrderBy(x => x.UpdatedAt).ToList(),
                CarryOver = all.Where(x => x.Status != WorkTaskStatus.Done && x.Status != WorkTaskStatus.Blocked && x.DueDate.HasValue && x.DueDate.Value.Date < day).OrderBy(x => x.DueDate).ToList()
            };
        }
        public async Task<string> GenerateTextAsync(DateTime date)
        {
            DailySummary summary = await GenerateAsync(date); IReadOnlyList<TaskHistory> history = await tasks.GetHistoryAsync(); StringBuilder text = new StringBuilder();
            text.Append(summary.Date.ToString("yyyy-MM-dd")).Append(" 업무 정리\r\n\r\n[완료]\r\n"); AppendTasks(text, summary.Completed, history, true);
            text.Append("\r\n[진행]\r\n"); AppendTasks(text, summary.InProgress, history, false);
            text.Append("\r\n[대기]\r\n"); AppendTasks(text, summary.Blocked, history, false);
            IList<WorkTask> todo = summary.Remaining.Where(x => x.Status == WorkTaskStatus.Todo).ToList(); text.Append("\r\n[남은 업무]\r\n"); AppendTasks(text, todo, history, false);
            return text.ToString().TrimEnd();
        }
        private static void AppendTasks(StringBuilder text, IEnumerable<WorkTask> items, IEnumerable<TaskHistory> history, bool includeMemo)
        {
            bool any = false; foreach (WorkTask task in items) { any = true; text.Append("- ").Append(task.Title).Append("\r\n"); if (task.Status == WorkTaskStatus.Blocked && !string.IsNullOrWhiteSpace(task.BlockedReason)) text.Append("  - ").Append(task.BlockedReason).Append("\r\n"); if (includeMemo) foreach (TaskHistory item in history.Where(x => x.TaskId == task.Id && x.ActionType == TaskHistoryAction.MemoAdded).OrderBy(x => x.CreatedAt)) text.Append("  - ").Append(item.Memo).Append("\r\n"); }
            if (!any) text.Append("- 없음\r\n");
        }
    }
}
