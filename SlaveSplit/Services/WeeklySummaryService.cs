using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Models;
namespace SlaveSplit.Services
{
    public sealed class WeeklySummaryService
    {
        private readonly TaskService tasks; private readonly IDateExpressionParser dates;
        public WeeklySummaryService(TaskService tasks, IDateExpressionParser dates = null) { this.tasks = tasks; this.dates = dates ?? new DateExpressionParser(); }
        public DateRange GetWeek(DateTime date) { return dates.GetWeek(date); }
        public Task<WeeklySummary> GetThisWeekAsync(DateTime date) { return GenerateAsync(dates.GetWeek(date)); }
        public Task<WeeklySummary> GetLastWeekAsync(DateTime date) { DateRange current = dates.GetWeek(date); return GenerateAsync(new DateRange { StartDate = current.StartDate.AddDays(-7), EndDate = current.EndDate.AddDays(-7) }); }
        public async Task<WeeklySummary> GenerateAsync(DateRange range, string keyword = null)
        {
            IReadOnlyList<WorkTask> completed = await tasks.QueryAsync(new TaskQuery { StartDate = range.StartDate, EndDate = range.EndDate.AddDays(1), UseCompletedDate = true, Keyword = keyword }); IReadOnlyList<WorkTask> created = await tasks.QueryAsync(new TaskQuery { StartDate = range.StartDate, EndDate = range.EndDate.AddDays(1), UseCreatedDate = true, Keyword = keyword }); IReadOnlyList<WorkTask> all = await tasks.GetAllAsync(); IEnumerable<WorkTask> scope = all.Where(x => string.IsNullOrWhiteSpace(keyword) || (x.Title ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 || (x.Description ?? "").IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            List<WorkTask> remaining = scope.Where(x => x.Status != WorkTaskStatus.Done && x.CreatedAt.Date <= range.EndDate.Date).OrderBy(x => x.DueDate ?? DateTime.MaxValue).ToList(); List<WorkTask> blocked = remaining.Where(x => x.Status == WorkTaskStatus.Blocked).ToList(); List<WorkTask> progress = remaining.Where(x => x.Status == WorkTaskStatus.InProgress).ToList(); List<WorkTask> carry = remaining.Where(x => x.CreatedAt.Date < range.StartDate.Date).ToList();
            List<DailyTaskGroup> groups = new List<DailyTaskGroup>(); for (DateTime day = range.StartDate.Date; day <= range.EndDate.Date; day = day.AddDays(1)) groups.Add(new DailyTaskGroup { Date = day, Tasks = completed.Where(x => x.CompletedAt.HasValue && x.CompletedAt.Value.Date == day).OrderBy(x => x.CompletedAt).ToList() });
            return new WeeklySummary { Range = range, CompletedTasks = completed.OrderBy(x => x.CompletedAt).ToList(), CreatedTasks = created.OrderBy(x => x.CreatedAt).ToList(), RemainingTasks = remaining, BlockedTasks = blocked, InProgressTasks = progress, CarryOverTasks = carry, DailySummaries = groups };
        }
    }
}
