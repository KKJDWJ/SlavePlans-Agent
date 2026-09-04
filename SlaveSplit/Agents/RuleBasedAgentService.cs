using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;
using Services = SlaveSplit.Services;
namespace SlaveSplit.Agents
{
    public sealed class RuleBasedAgentService : IAgentService
    {
        private readonly TaskService taskService; private ConversationContext context; private readonly IClock clock; private readonly IAgentLogger logger; private readonly DailySummaryService summaries; private readonly WeeklySummaryService weekly; private readonly WorkReportFormatter reportFormatter; private readonly IDateExpressionParser dateParser;
        public RuleBasedAgentService(TaskService taskService, ConversationContext context = null, IClock clock = null, IAgentLogger logger = null, DailySummaryService summaries = null) { this.taskService = taskService; this.context = context ?? new ConversationContext(); this.clock = clock ?? new SystemClock(); this.logger = logger ?? new AgentLogger(); this.summaries = summaries ?? new DailySummaryService(taskService, this.clock); dateParser = new DateExpressionParser(); weekly = new WeeklySummaryService(taskService, dateParser); reportFormatter = new WorkReportFormatter(taskService); }
        public async Task<AgentResponse> ProcessAsync(AgentRequest request)
        {
            string input = request == null ? "" : (request.Message ?? "").Trim();
            try
            {
                logger.Info("User Message", input.Length == 0 ? "(empty)" : "received:length=" + input.Length);
                AgentIntent intent = DetectIntent(input); logger.Info("Detected Intent", intent.ToString()); AgentResponse response;
                switch (intent)
                {
                    case AgentIntent.ShowThisWeek: response = await ShowWeeklyAsync(false, input); break;
                    case AgentIntent.ShowLastWeek: response = await ShowWeeklyAsync(true, input); break;
                    case AgentIntent.ShowWeekRemaining: response = await ShowWeekRemainingAsync(); break;
                    case AgentIntent.ShowDate: response = await ShowDateAsync(input); break;
                    case AgentIntent.ShowYesterday: response = await ShowCompletedAsync(clock.Now.Date.AddDays(-1), intent, "어제 완료한 업무"); break;
                    case AgentIntent.ShowCompletedToday: response = await ShowCompletedAsync(clock.Now.Date, intent, "오늘 완료한 업무"); break;
                    case AgentIntent.ShowToday:
                    case AgentIntent.ShowRemaining: response = await ShowRemainingAsync(intent); break;
                    case AgentIntent.StartTask:
                    case AgentIntent.CompleteTask:
                    case AgentIntent.BlockTask: response = await ChangeTaskAsync(input, intent); break;
                    case AgentIntent.ShowTask: response = await ShowTaskAsync(input); break;
                    case AgentIntent.CreateTask: response = await CreateTaskAsync(input); break;
                    default: response = Unknown(); break;
                }
                logger.Info("Agent Result", response.Intent + ",success=" + response.Success + ",task=" + (response.RelatedTaskId.HasValue ? response.RelatedTaskId.ToString() : "none")); return response;
            }
            catch (Exception ex) { logger.Error("Exception", ex); return new AgentResponse { Intent = AgentIntent.Unknown, Success = false, ErrorMessage = "Agent processing failed", Message = "업무 처리 중 문제가 발생했습니다.\n\n자세한 내용은 로그를 확인해주세요." }; }
        }
        public void ResetContext() { context = new ConversationContext(); }
        private AgentIntent DetectIntent(string input)
        {
            string text = Normalize(input);
            if ((text.Contains("이번 주") || text.Contains("이번주")) && ContainsAny(text, "안 끝", "남은", "미완료")) return AgentIntent.ShowWeekRemaining;
            if (text.Contains("지난 주") || text.Contains("지난주")) return AgentIntent.ShowLastWeek;
            if (text.Contains("이번 주") || text.Contains("이번주") || text.Contains("주간 업무")) return AgentIntent.ShowThisWeek;
            DateRange parsedRange; if (dateParser.TryParse(input, clock.Now, out parsedRange) && parsedRange.StartDate == parsedRange.EndDate && !text.Contains("오늘") && !text.Contains("어제")) return AgentIntent.ShowDate;
            if (text.Contains("어제") && ContainsAny(text, "뭐했", "한거", "완료", "작업 내역", "정리", "요약")) return AgentIntent.ShowYesterday;
            if (text.Contains("오늘") && ContainsAny(text, "뭐했", "한거", "완료한", "작업 내역", "정리", "요약")) return AgentIntent.ShowCompletedToday;
            if (text.Contains("퇴근") && ContainsAny(text, "정리", "요약")) return AgentIntent.ShowCompletedToday;
            if (ContainsAny(text, "오늘 뭐 해야", "오늘 할일", "오늘 할 일", "오늘 남은", "오늘 뭐 남", "할 거 뭐", "할거 뭐")) return text.Contains("남") ? AgentIntent.ShowRemaining : AgentIntent.ShowToday;
            if (ContainsAny(text, "완료", "끝났", "끝 ", "끝.", "끝")) return AgentIntent.CompleteTask;
            if (ContainsAny(text, "시작", "시작할게")) return AgentIntent.StartTask;
            if (ContainsAny(text, "blocked", "로그 대기", "반출 후", "나와야 가능", "대기 상태")) return AgentIntent.BlockTask;
            if (ContainsAny(text, "보여줘", "어떻게 됐", "관련 업무", "업무 검색")) return AgentIntent.ShowTask;
            if (ContainsAny(text, "해야", "할일 추가", "할 일 추가", "등록", "추가해", "필요") || Regex.IsMatch(text, "(분석|개발|확인)$")) return AgentIntent.CreateTask;
            return AgentIntent.Unknown;
        }
        private async Task<AgentResponse> CreateTaskAsync(string input)
        {
            bool today = Normalize(input).Contains("오늘"); string title = CleanCreateTitle(input); if (title.Length == 0) return Unknown();
            WorkTask task = await taskService.CreateTaskAsync(title, dueDate: today ? clock.Now.Date : (DateTime?)null);
            logger.Info("Task Action", "Create " + task.Id); string github = !string.IsNullOrWhiteSpace(task.ExternalId) ? "\n\nGitHub Issue\n#" + task.ExternalId + " 생성 완료" : taskService.GitHubCreationService != null && taskService.GitHubCreationService.IsAutoCreateEnabled ? "\n\nGitHub Issue 생성은 실패했습니다.\nLocal 업무는 정상적으로 저장되었습니다." : ""; return Success(AgentIntent.CreateTask, task.Id, "업무로 등록했습니다.\n\n" + task.Title + "\n\n상태  Todo\n일정  " + (today ? "오늘" : "미정") + github);
        }
        private async Task<AgentResponse> ChangeTaskAsync(string input, AgentIntent intent)
        {
            List<WorkTask> all = (await taskService.GetAllAsync()).Where(x => x.Status != WorkTaskStatus.Done || intent != AgentIntent.CompleteTask).ToList(); WorkTask target = null;
            Match numberMatch = Regex.Match(input, "(\\d+)\\s*번");
            if (numberMatch.Success)
            {
                int number = int.Parse(numberMatch.Groups[1].Value); Guid? id = context.Resolve(number);
                if (!id.HasValue) return Failure(intent, "현재 표시된 업무에서 " + number + "번을 찾을 수 없습니다.\n\n\"오늘 할 일\"이라고 입력하여 업무 목록을 다시 확인해주세요.");
                target = all.FirstOrDefault(x => x.Id == id.Value);
            }
            else
            {
                string query = CleanActionQuery(input, intent); List<WorkTask> matches = FindMatches(all, query);
                if (matches.Count > 1) return Ambiguous(intent, query, matches);
                if (matches.Count == 1) target = matches[0];
            }
            if (target == null) return Failure(intent, "관련 업무를 찾지 못했습니다.\n\n\"오늘 할 일\"이라고 입력하여 업무 목록을 확인해주세요.");
            if (intent == AgentIntent.StartTask) await taskService.StartAsync(target, "Messenger에서 업무 시작");
            else if (intent == AgentIntent.CompleteTask) await taskService.CompleteAsync(target, "Messenger에서 업무 완료");
            else await taskService.BlockAsync(target, ExtractBlockReason(input));
            logger.Info("Task Action", intent + " " + target.Id); string sync = target.Source == TaskSource.GitHub && intent == AgentIntent.CompleteTask ? target.GitHubSyncStatus == Services.GitHub.GitHubSyncStatus.Synced ? "\n\nGitHub Issue #" + target.ExternalId + "도 Closed 처리했습니다." : "\n\nGitHub 반영은 대기 중입니다." : ""; string message = intent == AgentIntent.StartTask ? "업무를 시작했습니다.\n\n" + target.Title : intent == AgentIntent.CompleteTask ? "완료했습니다.\n\n" + target.Title + "\n\n완료  " + target.CompletedAt.Value.ToString("HH:mm") + sync : "업무를 대기 상태로 변경했습니다.\n\n" + target.Title + "\n\n사유  " + target.BlockedReason;
            return Success(intent, target.Id, message);
        }
        private async Task<AgentResponse> ShowRemainingAsync(AgentIntent intent)
        {
            List<WorkTask> tasks = (await taskService.GetAllAsync()).Where(x => x.Status != WorkTaskStatus.Done).OrderBy(x => x.Status).ThenBy(x => x.CreatedAt).ToList(); context.SetDisplayedTasks(tasks, clock.Now);
            if (tasks.Count == 0) return Success(intent, null, "오늘 남은 업무가 없습니다.");
            StringBuilder text = new StringBuilder(); text.Append("오늘 남은 업무는 ").Append(tasks.Count).Append("건입니다.\n");
            for (int i = 0; i < tasks.Count; i++) { WorkTask task = tasks[i]; text.Append("\n").Append(i + 1).Append(". ").Append(task.Title).Append("\n   상태: ").Append(task.StatusText); if (task.Status == WorkTaskStatus.Blocked && !string.IsNullOrWhiteSpace(task.BlockedReason)) text.Append("\n   사유: ").Append(task.BlockedReason); }
            return Success(intent, null, text.ToString());
        }
        private async Task<AgentResponse> ShowCompletedAsync(DateTime date, AgentIntent intent, string label)
        {
            List<WorkTask> tasks = (await taskService.GetAllAsync()).Where(x => x.Status == WorkTaskStatus.Done && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == date.Date).OrderBy(x => x.CompletedAt).ToList(); context.SetDisplayedTasks(tasks, clock.Now);
            if (tasks.Count == 0) return Success(intent, null, label + "가 없습니다."); string summaryText = await summaries.GenerateTextAsync(date); return Success(intent, null, summaryText);
        }
        private async Task<AgentResponse> ShowWeeklyAsync(bool lastWeek, string input)
        {
            WeeklySummary summary = lastWeek ? await weekly.GetLastWeekAsync(clock.Now.Date) : await weekly.GetThisWeekAsync(clock.Now.Date); string keyword = ExtractWeeklyKeyword(input); if (!string.IsNullOrWhiteSpace(keyword)) summary = await weekly.GenerateAsync(summary.Range, keyword); string report = await reportFormatter.FormatAsync(summary, ReportFormat.Detailed); AgentIntent intent = lastWeek ? AgentIntent.ShowLastWeek : AgentIntent.ShowThisWeek; return Success(intent, null, report);
        }
        private async Task<AgentResponse> ShowDateAsync(string input) { DateRange range; if (!dateParser.TryParse(input, clock.Now, out range)) return Failure(AgentIntent.ShowDate, "조회할 날짜를 확인하지 못했습니다."); WeeklySummary summary = await weekly.GenerateAsync(range); return Success(AgentIntent.ShowDate, null, await reportFormatter.FormatAsync(summary, ReportFormat.Detailed)); }
        private async Task<AgentResponse> ShowWeekRemainingAsync() { WeeklySummary summary = await weekly.GetThisWeekAsync(clock.Now.Date); StringBuilder text = new StringBuilder("이번 주 미완료 업무는 " + summary.RemainingCount + "건입니다."); foreach (WorkTask task in summary.RemainingTasks) { text.Append("\n\n").Append(task.Status == WorkTaskStatus.Blocked ? "⚠ " : "→ ").Append(task.Title); if (task.Status == WorkTaskStatus.Blocked && !string.IsNullOrWhiteSpace(task.BlockedReason)) text.Append("\n  ").Append(task.BlockedReason); } return Success(AgentIntent.ShowWeekRemaining, null, text.ToString()); }
        private static string ExtractWeeklyKeyword(string input) { string value = Normalize(input); value = value.Replace("이번 주", "").Replace("이번주", "").Replace("지난 주", "").Replace("지난주", "").Replace("관련", "").Replace("뭐했지", "").Replace("뭐했어", "").Replace("업무", "").Replace("정리해줘", "").Trim(); return value; }
        private async Task<AgentResponse> ShowTaskAsync(string input)
        {
            string query = input.Replace("보여줘", "").Replace("관련 업무", "").Replace("업무 검색", "").Replace("어떻게 됐지", "").Replace("어떻게 됐어", "").Trim(); List<WorkTask> matches = FindMatches((await taskService.GetAllAsync()).ToList(), query); context.SetDisplayedTasks(matches, clock.Now); if (matches.Count == 0) return Failure(AgentIntent.ShowTask, "관련 업무를 찾지 못했습니다."); StringBuilder text = new StringBuilder(); text.Append(query).Append(" 관련 업무는 ").Append(matches.Count).Append("건입니다.\n"); for (int i = 0; i < matches.Count; i++) text.Append("\n").Append(i + 1).Append(". ").Append(matches[i].Title).Append("\n   상태: ").Append(matches[i].StatusText); return Success(AgentIntent.ShowTask, null, text.ToString());
        }
        private AgentResponse Ambiguous(AgentIntent intent, string query, List<WorkTask> matches) { context.SetDisplayedTasks(matches, clock.Now); StringBuilder text = new StringBuilder(); text.Append(query).Append(" 관련 업무가 ").Append(matches.Count).Append("건 있습니다.\n"); for (int i = 0; i < matches.Count; i++) text.Append("\n").Append(i + 1).Append(". ").Append(matches[i].Title); text.Append("\n\n처리할 번호를 입력해주세요."); return Failure(intent, text.ToString()); }
        private static List<WorkTask> FindMatches(List<WorkTask> tasks, string query) { string normalized = Normalize(query); string[] tokens = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 1).ToArray(); if (normalized.Length == 0) return new List<WorkTask>(); List<WorkTask> exact = tasks.Where(x => Normalize(x.Title).Contains(normalized)).ToList(); if (exact.Count > 0) return exact; return tasks.Where(x => tokens.Length > 0 && tokens.All(t => Normalize(x.Title).Contains(t))).ToList(); }
        private static string CleanCreateTitle(string input) { string value = input.Trim(); value = Regex.Replace(value, "^(오늘|내일)\\s*", ""); value = Regex.Replace(value, "(해야\\s*(함|해|돼|됨)?|할\\s*일\\s*추가|할일\\s*추가|등록(해|해줘)?|추가해줘|추가해|필요(해)?)[.!?\\s]*$", ""); return value.Trim(' ', '.', '!', '?'); }
        private static string CleanActionQuery(string input, AgentIntent intent) { string value = Regex.Replace(input, "\\d+\\s*번", ""); if (intent == AgentIntent.CompleteTask) value = Regex.Replace(value, "(완료(했어|해줘|처리)?|끝났어|끝)[.!?\\s]*$", ""); else if (intent == AgentIntent.StartTask) value = Regex.Replace(value, "(시작(할게|해줘)?)[.!?\\s]*$", ""); else value = Regex.Replace(value, "(건)?\\s*(로그\\s*)?(반출\\s*후\\s*가능|대기|나와야\\s*가능|blocked)[.!?\\s]*$", "", RegexOptions.IgnoreCase); return value.Trim(); }
        private static string ExtractBlockReason(string input) { string text = Normalize(input); if (text.Contains("반출")) return "로그 반출 필요"; if (text.Contains("로그 대기") || text.Contains("나와야")) return "로그 대기"; return "외부 조건 확인 필요"; }
        private static string Normalize(string value) { return (value ?? "").Trim().ToLowerInvariant().Replace("?", "").Replace("!", "").Replace(".", ""); }
        private static bool ContainsAny(string text, params string[] values) { return values.Any(text.Contains); }
        private static AgentResponse Success(AgentIntent intent, Guid? id, string message) { return new AgentResponse { Intent = intent, RelatedTaskId = id, Message = message, Success = true }; }
        private static AgentResponse Failure(AgentIntent intent, string message) { return new AgentResponse { Intent = intent, Message = message, Success = false }; }
        private static AgentResponse Unknown() { return Failure(AgentIntent.Unknown, "아직 해당 요청을 이해하지 못했습니다.\n\n현재 사용할 수 있는 명령:\n\n• 업무 등록\n• 업무 시작\n• 업무 완료\n• 업무 대기\n• 오늘 할 일\n• 오늘 한 일\n• 어제 한 일"); }
    }
}
