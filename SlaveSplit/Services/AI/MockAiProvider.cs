using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace SlaveSplit.Services.AI
{
    public sealed class MockAiProvider : IAiProvider
    {
        private AiProviderStatus status = AiProviderStatus.Connected; public bool FailNextRequest { get; set; } public bool ReturnInvalidJson { get; set; } public string ResponseOverride { get; set; } public int DelayMilliseconds { get; set; }
        public string Name { get { return "Mock AI"; } } public AiProviderStatus Status { get { return status; } } public event EventHandler StatusChanged;
        public async Task<AiResponse> SendAsync(AiRequest request, CancellationToken cancellationToken)
        {
            Stopwatch watch = Stopwatch.StartNew(); try { if (DelayMilliseconds > 0) await Task.Delay(DelayMilliseconds, cancellationToken); if (FailNextRequest) { FailNextRequest = false; throw new InvalidOperationException("Forced mock failure."); } SetStatus(AiProviderStatus.Connected); string text = request.UserMessage ?? ""; if (ReturnInvalidJson) return Result(true, "업무를 추가하세요.", watch.ElapsedMilliseconds, null);
                if (ResponseOverride != null) return Result(true, ResponseOverride, watch.ElapsedMilliseconds, null); List<AgentCommand> batch = CreateBatch(text, request.CurrentDateTime); if (batch != null) return Result(true, JsonConvert.SerializeObject(new { commands = batch.Select(ToContract).ToList(), confidence = .96, requiresConfirmation = false }), watch.ElapsedMilliseconds, null); AgentCommand command = CreateCommand(text, request.CurrentDateTime); return Result(true, JsonConvert.SerializeObject(ToContract(command)), watch.ElapsedMilliseconds, null);
            } catch (OperationCanceledException) { SetStatus(AiProviderStatus.Disconnected); return Result(false, null, watch.ElapsedMilliseconds, "Timeout or cancellation"); } catch (Exception ex) { SetStatus(AiProviderStatus.Error); return Result(false, null, watch.ElapsedMilliseconds, ex.Message); }
        }
        public Task<AiResponse> TestConnectionAsync(CancellationToken cancellationToken) { SetStatus(AiProviderStatus.Connected); return Task.FromResult(new AiResponse { Success = true, Content = "Mock provider ready", ProviderName = Name, ElapsedMilliseconds = 0 }); }
        private static AgentCommand CreateCommand(string input, DateTime now) { string text = input.Trim(); AgentCommand command = new AgentCommand { Action = AgentCommandAction.Unknown, Confidence = .99 }; if (text.Contains("어제") && (text.Contains("뭐했") || text.Contains("완료"))) command.Action = AgentCommandAction.ShowYesterday; else if (text.Contains("오늘") && (text.Contains("뭐했") || text.Contains("완료한"))) command.Action = AgentCommandAction.ShowCompletedToday; else if (text.Contains("오늘") && (text.Contains("할 일") || text.Contains("할일") || text.Contains("남"))) command.Action = AgentCommandAction.ShowRemaining; else if (text.Contains("완료") || text.Contains("끝")) { command.Action = AgentCommandAction.CompleteTask; command.Title = Clean(text, "완료", "끝났어", "끝"); } else if (text.Contains("시작")) { command.Action = AgentCommandAction.StartTask; command.Title = Clean(text, "시작할게", "시작"); } else if (text.ToLowerInvariant().Contains("blocked") || text.Contains("대기") || text.Contains("반출 후")) { command.Action = AgentCommandAction.BlockTask; command.Title = Regex.Replace(text, "(건)?\\s*(로그\\s*)?(반출\\s*후\\s*가능|대기|blocked).*", "", RegexOptions.IgnoreCase).Trim(); command.BlockedReason = text.Contains("반출") ? "로그 반출 필요" : "외부 조건 확인 필요"; } else if (text.Contains("해야") || text.Contains("등록") || text.Contains("추가")) { command.Action = AgentCommandAction.CreateTask; command.Title = Regex.Replace(text, "^(오늘|내일)\\s*", ""); command.Title = Regex.Replace(command.Title, "(해야.*|등록.*|추가.*)$", "").Trim(); command.DueDate = text.Contains("내일") ? now.Date.AddDays(1) : text.Contains("오늘") ? now.Date : (DateTime?)null; } return command; }
        private static List<AgentCommand> CreateBatch(string text, DateTime now)
        {
            List<AgentCommand> commands = new List<AgentCommand>(); string normalized = text.Replace("\r", " ").Replace("\n", " ");
            if ((normalized.Contains("완료") || normalized.Contains("끝났")) && (normalized.Contains("필요") || normalized.Contains("코드 요청") || normalized.Contains("Plan B") || normalized.Contains("플랜 B"))) { string baseTitle = Regex.Split(normalized, "완료|끝났")[0].Trim(' ', '.'); commands.Add(new AgentCommand { Action = AgentCommandAction.CompleteTask, Confidence = .97, Title = baseTitle, Memo = "로그 분석 완료" }); if (normalized.Contains("코드 요청")) commands.Add(new AgentCommand { Action = AgentCommandAction.AddMemo, Confidence = .98, Title = baseTitle, Memo = "담당자에게 코드 요청" }); if (normalized.Contains("Plan B") || normalized.Contains("플랜 B")) commands.Add(new AgentCommand { Action = AgentCommandAction.CreateFollowup, Confidence = .96, Title = "Plan B 방식 개발", Description = ExtractFollowUpDescription(text), DueDate = normalized.Contains("내일") ? now.Date.AddDays(1) : (DateTime?)null }); return commands; }
            if (normalized.Contains("이미 되어") || normalized.Contains("이미 적용")) { string title = Regex.Split(normalized, "확인했는데|이미")[0].Trim(' ', '.'); commands.Add(new AgentCommand { Action = AgentCommandAction.CompleteTask, Confidence = .95, Title = title }); commands.Add(new AgentCommand { Action = AgentCommandAction.AddMemo, Confidence = .96, Title = title, Memo = "기존 로직에 이미 적용되어 있음 확인. 별도 코드 수정 불필요." }); return commands; }
            if (normalized.Contains("로그 나왔") || normalized.Contains("로그가 나왔")) { commands.Add(new AgentCommand { Action = AgentCommandAction.UnblockTask, Confidence = .96, Title = Regex.Split(normalized, "로그")[0].Trim() }); return commands; }
            if (normalized.Contains("프로그램 개발이야") || normalized.Contains("내용 업데이트")) { commands.Add(new AgentCommand { Action = AgentCommandAction.UpdateTask, Confidence = .94, Title = Regex.Split(normalized, "는|은")[0].Trim(), Description = text.Trim() }); return commands; }
            if ((normalized.Contains("Plan B") || normalized.Contains("플랜 B")) && normalized.Contains("필요")) { commands.Add(new AgentCommand { Action = AgentCommandAction.CreateTask, Confidence = .94, Title = "Plan B 방식 개발" }); return commands; }
            return null;
        }
        private static string ExtractFollowUpDescription(string text) { int index = text.IndexOf("Plan B", StringComparison.OrdinalIgnoreCase); return index < 0 ? null : text.Substring(index).Trim(); }
        private static string Clean(string text, params string[] words) { foreach (string word in words) text = text.Replace(word, ""); return Regex.Replace(text, "\\d+\\s*번", "").Trim(' ', '.', '!', '?'); }
        private static object ToContract(AgentCommand command) { return new { action = ToExternal(command.Action), confidence = command.Confidence, taskId = command.TaskId, title = command.Title, description = command.Description, status = command.Status, priority = command.Priority, dueDate = command.DueDate, blockedReason = command.BlockedReason, memo = command.Memo }; }
        private static string ToExternal(AgentCommandAction action) { return Regex.Replace(action.ToString(), "([a-z])([A-Z])", "$1_$2").ToUpperInvariant(); }
        private AiResponse Result(bool success, string content, long elapsed, string error) { return new AiResponse { Success = success, Content = content, RawContent = content, ErrorMessage = error, ElapsedMilliseconds = elapsed, ProviderName = Name, ModelName = "mock-command-v1" }; }
        private void SetStatus(AiProviderStatus value) { if (status == value) return; status = value; EventHandler handler = StatusChanged; if (handler != null) handler(this, EventArgs.Empty); }
    }
}
