using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlaveSplit.Models;
namespace SlaveSplit.Services
{
    public sealed class WorkReportFormatter
    {
        private readonly TaskService tasks; public WorkReportFormatter(TaskService tasks) { this.tasks = tasks; }
        public async Task<string> FormatAsync(WeeklySummary summary, ReportFormat format)
        {
            if (format == ReportFormat.Compact) return FormatCompact(summary); IReadOnlyList<TaskHistory> history = await tasks.GetHistoryAsync(); StringBuilder text = new StringBuilder(); text.Append("[").Append(summary.StartDate.ToString("yyyy.MM.dd")).Append(" ~ ").Append(summary.EndDate.ToString("yyyy.MM.dd")).Append(" 업무 정리]\r\n\r\n■ 완료\r\n"); Append(text, summary.CompletedTasks, history, true); text.Append("\r\n■ 진행\r\n"); Append(text, summary.InProgressTasks, history, false); text.Append("\r\n■ 대기\r\n"); Append(text, summary.BlockedTasks, history, false); text.Append("\r\n■ 남은 업무\r\n"); Append(text, summary.RemainingTasks.Where(x => x.Status == WorkTaskStatus.Todo), history, false); return text.ToString().TrimEnd();
        }
        private static string FormatCompact(WeeklySummary summary) { StringBuilder text = new StringBuilder("[금주 업무]\r\n\r\n완료 " + summary.CompletedCount + " / 진행 " + summary.InProgressCount + " / 대기 " + summary.BlockedCount + "\r\n"); foreach (WorkTask task in summary.CompletedTasks) text.Append("\r\n- ").Append(task.Title).Append(" 완료"); foreach (WorkTask task in summary.InProgressTasks) text.Append("\r\n- ").Append(task.Title).Append(" 진행"); foreach (WorkTask task in summary.BlockedTasks) text.Append("\r\n- ").Append(task.Title).Append(" 대기"); return text.ToString(); }
        private static void Append(StringBuilder text, IEnumerable<WorkTask> items, IEnumerable<TaskHistory> history, bool memo) { bool any = false; foreach (WorkTask task in items) { any = true; text.Append("- ").Append(task.Title).Append("\r\n"); if (task.Status == WorkTaskStatus.Blocked && !string.IsNullOrWhiteSpace(task.BlockedReason)) text.Append("  - ").Append(task.BlockedReason).Append("\r\n"); if (memo) foreach (TaskHistory item in history.Where(x => x.TaskId == task.Id && x.ActionType == TaskHistoryAction.MemoAdded).OrderBy(x => x.CreatedAt)) text.Append("  - ").Append(item.Memo).Append("\r\n"); } if (!any) text.Append("- 없음\r\n"); }
    }
}
