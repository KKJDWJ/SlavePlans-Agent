using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;
using Services = SlaveSplit.Services;
using SlaveSplit.Services.AI;
namespace SlaveSplit.Agents
{
    public sealed class HybridAgentService : IAgentService
    {
        private readonly AiProviderManager providers; private readonly IAiContextBuilder contextBuilder; private readonly IAgentCommandParser parser; private readonly RuleBasedAgentService fallback; private readonly TaskService tasks; private readonly AiProviderOptions options; private readonly IAgentLogger logger; private readonly ConversationContext context; private readonly IClock clock; private PendingCommandContext pending;
        public HybridAgentService(AiProviderManager providers, IAiContextBuilder contextBuilder, IAgentCommandParser parser, RuleBasedAgentService fallback, TaskService tasks, AiProviderOptions options, IAgentLogger logger, ConversationContext context = null, IClock clock = null) { this.providers = providers; this.contextBuilder = contextBuilder; this.parser = parser; this.fallback = fallback; this.tasks = tasks; this.options = options; this.logger = logger; this.context = context ?? new ConversationContext(); this.clock = clock ?? new SystemClock(); }
        public async Task<AgentResponse> ProcessAsync(AgentRequest request)
        {
            AgentResponse pendingResult = await TryPendingAsync(request); if (pendingResult != null) return pendingResult; Stopwatch watch = Stopwatch.StartNew();
            try { AiRequest aiRequest = await contextBuilder.BuildAsync(request); using (CancellationTokenSource timeout = new CancellationTokenSource()) { timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds))); IAiProvider provider = providers.Current; logger.Info("AI Request", "provider=" + provider.Name + ",time=" + DateTime.Now.ToString("O")); AiResponse response = await provider.SendAsync(aiRequest, timeout.Token); logger.Info("AI Response", "provider=" + provider.Name + ",elapsed=" + response.ElapsedMilliseconds + ",success=" + response.Success); if (!response.Success) return await FallbackAsync(request, "provider failure"); AgentCommandBatchParseResult parsed = parser.ParseBatch(response.Content); logger.Info("Parser Result", "success=" + parsed.Success + (parsed.Success ? ",commands=" + parsed.Batch.Commands.Count : "")); if (!parsed.Success || parsed.Batch.Commands.Any(x => x.Confidence < options.ConfidenceThreshold || x.Action == AgentCommandAction.Unknown)) return await FallbackAsync(request, parsed.Success ? "low confidence or unknown" : "parse failure"); AgentBatchResult batch = await ExecuteBatchAsync(parsed.Batch, request.Message); return BuildBatchResponse(batch); } }
            catch (Exception ex) { logger.Error("Hybrid Agent Exception", ex); return await FallbackAsync(request, "exception"); } finally { logger.Info("Hybrid Agent Elapsed", watch.ElapsedMilliseconds.ToString()); }
        }
        public void ResetContext() { fallback.ResetContext(); context.Clear(); pending = null; }
        private async Task<AgentResponse> FallbackAsync(AgentRequest request, string reason) { logger.Info("Fallback", reason); return await fallback.ProcessAsync(request); }
        private async Task<AgentBatchResult> ExecuteBatchAsync(AgentCommandBatch batch, string original)
        {
            AgentBatchResult result = new AgentBatchResult(); Guid? lastTarget = null; logger.Info("Batch", "commands=" + batch.Commands.Count + ",confirmation=" + batch.RequiresConfirmation);
            foreach (AgentCommand command in batch.Commands)
            {
                if (!command.TaskId.HasValue && lastTarget.HasValue && (command.Action == AgentCommandAction.AddMemo || command.Action == AgentCommandAction.CreateFollowup)) command.TaskId = lastTarget;
                AgentCommandResult item; try { item = await ExecuteCommandAsync(command, original); } catch (Exception ex) { item = new AgentCommandResult { Action = command.Action, Success = false, ErrorMessage = ex.Message, Message = "처리 실패: " + command.Action }; logger.Error("Batch Command Failure", ex); }
                result.Results.Add(item); if (item.Success) { result.SuccessCount++; if (command.Action != AgentCommandAction.CreateFollowup && item.TaskId.HasValue) lastTarget = item.TaskId; } else result.FailureCount++; logger.Info("Batch Command", command.Action + ",success=" + item.Success);
                if (pending != null) break;
            }
            return result;
        }
        private async Task<AgentCommandResult> ExecuteCommandAsync(AgentCommand command, string original)
        {
            if (command.Action == AgentCommandAction.CreateTask) { var existing = await tasks.GetAllAsync(); WorkTask duplicate = existing.FirstOrDefault(x => Similar(x.Title, command.Title)); if (duplicate != null) return Fail(command, "비슷한 업무가 이미 있습니다.\n" + duplicate.Title + "\n새로 등록하지 않았습니다."); WorkTask task = await tasks.CreateTaskAsync(command.Title, command.Description, null, ParsePriority(command.Priority), command.DueDate); string github = !string.IsNullOrWhiteSpace(task.ExternalId) ? "\nGitHub Issue #" + task.ExternalId + " 생성 완료" : tasks.GitHubCreationService != null && tasks.GitHubCreationService.IsAutoCreateEnabled ? "\nGitHub Issue 생성 실패 · Local 업무는 정상 저장됨" : ""; return Ok(command, task.Id, "업무 등록: " + task.Title + github); }
            if (command.Action == AgentCommandAction.ShowToday || command.Action == AgentCommandAction.ShowRemaining) { var list = (await tasks.GetAllAsync()).Where(x => x.Status != WorkTaskStatus.Done).OrderBy(x => x.Status).ToList(); context.SetDisplayedTasks(list, clock.Now); return Ok(command, null, BuildRemaining(list)); }
            if (command.Action == AgentCommandAction.ShowCompletedToday) return Ok(command, null, await BuildCompletedAsync(clock.Now.Date, "오늘 완료한 업무"));
            if (command.Action == AgentCommandAction.ShowYesterday) return Ok(command, null, await BuildCompletedAsync(clock.Now.Date.AddDays(-1), "어제 완료한 업무"));
            if (command.Action == AgentCommandAction.SearchTask) { var found = await FindCandidatesAsync(command.Title); context.SetDisplayedTasks(found, clock.Now); return Ok(command, null, BuildCandidates("검색 결과", found)); }
            List<WorkTask> candidates = await ResolveCandidatesAsync(command, original); if (candidates.Count > 1) { pending = new PendingCommandContext { Action = command.Action, CandidateTaskIds = candidates.Select(x => x.Id).ToList(), Memo = command.Memo, CreatedAt = clock.Now }; context.SetDisplayedTasks(candidates, clock.Now); return Fail(command, BuildCandidates("대상 업무를 선택해주세요", candidates)); }
            WorkTask target = candidates.SingleOrDefault(); if (target == null) return Fail(command, "요청한 업무를 찾을 수 없어 변경하지 않았습니다.");
            if (command.Action == AgentCommandAction.StartTask) await tasks.StartAsync(target, command.Memo);
            else if (command.Action == AgentCommandAction.CompleteTask) await tasks.CompleteAsync(target, command.Memo);
            else if (command.Action == AgentCommandAction.BlockTask) await tasks.BlockAsync(target, command.BlockedReason);
            else if (command.Action == AgentCommandAction.UnblockTask) await tasks.UnblockAsync(target, false, command.Memo);
            else if (command.Action == AgentCommandAction.AddMemo) await tasks.AddMemoAsync(target, command.Memo);
            else if (command.Action == AgentCommandAction.UpdateTask) { if (!string.IsNullOrWhiteSpace(command.Description)) target.Description = command.Description; if (command.DueDate.HasValue) target.DueDate = command.DueDate; if (!string.IsNullOrWhiteSpace(command.Priority)) target.Priority = ParsePriority(command.Priority); await tasks.UpdateAsync(target); }
            else if (command.Action == AgentCommandAction.CreateFollowup) { string title = BuildFollowUpTitle(target, command.Title); var all = await tasks.GetAllAsync(); WorkTask duplicate = all.FirstOrDefault(x => x.ParentTaskId == target.Id && Similar(x.Title, title)); if (duplicate != null) return Fail(command, "비슷한 후속 업무가 이미 있습니다.\n" + duplicate.Title + "\n새로 등록하지 않았습니다."); WorkTask child = await tasks.CreateFollowUpAsync(target, title, command.Description, ParsePriority(command.Priority), command.DueDate); return Ok(command, child.Id, "후속 업무 등록: " + child.Title); }
            else return Fail(command, "지원하지 않는 명령입니다.");
            return Ok(command, target.Id, ActionMessage(command.Action, target));
        }
        private async Task<AgentResponse> TryPendingAsync(AgentRequest request)
        {
            if (pending == null) return null; if ((clock.Now - pending.CreatedAt).TotalMinutes > 30) { pending = null; return null; } Match match = Regex.Match(request.Message ?? "", "^\\s*(\\d+)\\s*번?\\s*[.!]?$" ); if (!match.Success) return null; int number = int.Parse(match.Groups[1].Value); if (number < 1 || number > pending.CandidateTaskIds.Count) return new AgentResponse { Success = false, Intent = AgentIntent.Unknown, Message = "현재 후보에서 " + number + "번을 찾을 수 없습니다." }; Guid id = pending.CandidateTaskIds[number - 1]; AgentCommand command = new AgentCommand { Action = pending.Action, TaskId = id, Memo = pending.Memo, Confidence = 1 }; pending = null; AgentCommandResult result = await ExecuteCommandAsync(command, request.Message); return new AgentResponse { Success = result.Success, Intent = MapIntent(command.Action), RelatedTaskId = result.TaskId, Message = result.Message };
        }
        private async Task<List<WorkTask>> ResolveCandidatesAsync(AgentCommand command, string original)
        {
            var all = await tasks.GetAllAsync(); if (command.TaskId.HasValue) { WorkTask exact = all.FirstOrDefault(x => x.Id == command.TaskId.Value); return exact == null ? new List<WorkTask>() : new List<WorkTask> { exact }; } Match number = Regex.Match(original ?? "", "(\\d+)\\s*번"); if (number.Success) { Guid? id = context.Resolve(int.Parse(number.Groups[1].Value)); WorkTask numbered = id.HasValue ? all.FirstOrDefault(x => x.Id == id.Value) : null; if (numbered != null) return new List<WorkTask> { numbered }; } return await FindCandidatesAsync(command.Title);
        }
        private async Task<List<WorkTask>> FindCandidatesAsync(string title) { var all = await tasks.GetAllAsync(); if (string.IsNullOrWhiteSpace(title)) return new List<WorkTask>(); string[] tokens = Normalize(title).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 1 && x != "로그" && x != "분석" && x != "확인").ToArray(); return all.Where(x => tokens.Length > 0 && tokens.All(t => Normalize(x.Title).Contains(t))).ToList(); }
        private async Task<string> BuildCompletedAsync(DateTime date, string label) { var all = await tasks.GetAllAsync(); var list = all.Where(x => x.Status == WorkTaskStatus.Done && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == date.Date).OrderBy(x => x.CompletedAt).ToList(); StringBuilder text = new StringBuilder(label + "는 " + list.Count + "건입니다."); foreach (WorkTask task in list) { text.Append("\n\n✓ ").Append(task.CompletedAt.Value.ToString("HH:mm")).Append("\n  ").Append(task.Title); var history = await tasks.GetHistoryAsync(task.Id); foreach (TaskHistory memo in history.Where(x => x.ActionType == TaskHistoryAction.MemoAdded)) text.Append("\n  ").Append(memo.Memo); } return text.ToString(); }
        private static string BuildRemaining(List<WorkTask> list) { StringBuilder text = new StringBuilder("오늘 남은 업무는 " + list.Count + "건입니다."); foreach (WorkTaskStatus status in new[] { WorkTaskStatus.InProgress, WorkTaskStatus.Todo, WorkTaskStatus.Blocked }) { var section = list.Where(x => x.Status == status).ToList(); if (section.Count == 0) continue; text.Append("\n\n").Append(status == WorkTaskStatus.InProgress ? "IN PROGRESS" : status.ToString().ToUpperInvariant()); foreach (WorkTask task in section) { int number = list.IndexOf(task) + 1; text.Append("\n\n").Append(number).Append(". ").Append(task.Title); if (status == WorkTaskStatus.Blocked && !string.IsNullOrWhiteSpace(task.BlockedReason)) text.Append("\n   └ ").Append(task.BlockedReason); } } return text.ToString(); }
        private static string BuildCandidates(string label, List<WorkTask> list) { StringBuilder text = new StringBuilder(label + "\n"); for (int i = 0; i < list.Count; i++) text.Append("\n").Append(i + 1).Append(". ").Append(list[i].Title); return text.ToString(); }
        private static AgentResponse BuildBatchResponse(AgentBatchResult batch) { StringBuilder text = new StringBuilder(batch.FailureCount == 0 ? "처리했습니다." : batch.SuccessCount > 0 ? "일부 작업을 처리했습니다." : "처리하지 못했습니다."); foreach (AgentCommandResult item in batch.Results) text.Append("\n\n").Append(item.Success ? "✓ " : "✕ ").Append(item.Message ?? item.ErrorMessage); AgentCommandResult last = batch.Results.LastOrDefault(x => x.TaskId.HasValue); return new AgentResponse { Success = batch.FailureCount == 0, Intent = AgentIntent.Unknown, RelatedTaskId = last == null ? (Guid?)null : last.TaskId, Message = text.ToString() }; }
        private static AgentCommandResult Ok(AgentCommand command, Guid? id, string message) { return new AgentCommandResult { Action = command.Action, Success = true, TaskId = id, Message = message }; } private static AgentCommandResult Fail(AgentCommand command, string message) { return new AgentCommandResult { Action = command.Action, Success = false, Message = message, ErrorMessage = message }; }
        private static string ActionMessage(AgentCommandAction action, WorkTask task) { string sync = task.Source == TaskSource.GitHub && (action == AgentCommandAction.CompleteTask || action == AgentCommandAction.AddMemo) ? task.GitHubSyncStatus == Services.GitHub.GitHubSyncStatus.Synced ? " · GitHub #" + task.ExternalId + " 반영 완료" : " · GitHub 반영 대기 중" : ""; if (action == AgentCommandAction.CompleteTask) return "완료: " + task.Title + sync; if (action == AgentCommandAction.AddMemo) return "메모: " + task.Title + sync; if (action == AgentCommandAction.BlockTask) return "대기: " + task.Title + " · " + task.BlockedReason; if (action == AgentCommandAction.UnblockTask) return "대기 해제: " + task.Title; if (action == AgentCommandAction.UpdateTask) return "업무 수정: " + task.Title; return "업무 시작: " + task.Title; }
        private static TaskPriority ParsePriority(string value) { TaskPriority priority; return Enum.TryParse<TaskPriority>(value ?? "Normal", true, out priority) ? priority : TaskPriority.Normal; }
        private static string BuildFollowUpTitle(WorkTask parent, string title) { if (string.IsNullOrWhiteSpace(title)) return title; string[] prefix = parent.Title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Take(2).ToArray(); return prefix.Any(x => title.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0) ? title.Trim() : string.Join(" ", prefix) + " " + title.Trim(); }
        private static bool Similar(string left, string right) { string a = Normalize(left), b = Normalize(right); return a == b || a.Contains(b) || b.Contains(a); } private static string Normalize(string value) { return Regex.Replace((value ?? "").ToLowerInvariant(), "[^0-9a-z가-힣]+", " ").Trim(); }
        private static AgentIntent MapIntent(AgentCommandAction action) { if (action == AgentCommandAction.StartTask) return AgentIntent.StartTask; if (action == AgentCommandAction.CompleteTask) return AgentIntent.CompleteTask; if (action == AgentCommandAction.BlockTask || action == AgentCommandAction.UnblockTask) return AgentIntent.BlockTask; return AgentIntent.Unknown; }
    }
}
