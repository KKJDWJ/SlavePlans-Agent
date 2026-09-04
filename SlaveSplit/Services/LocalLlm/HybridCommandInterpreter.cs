using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SlaveSplit.Agents;
using SlaveSplit.Models;

namespace SlaveSplit.Services.LocalLlm
{
    public enum HybridCommandSource { PARSER, LLM, UNRESOLVED }

    public sealed class HybridCommandResult
    {
        public HybridCommandSource Source { get; set; }
        public string Reason { get; set; }
        public LlmTaskParseResult Result { get; set; }
        public AgentDryRunResult ParserResult { get; set; }
        public LocalLlmResponse LlmResponse { get; set; }
        public bool FallbackCalled { get; set; }
        public bool NeedsClarification { get; set; }
        public bool MultiActionSchemaLimit { get; set; }
        public long ParserLatencyMs { get; set; }
        public long HybridLatencyMs { get; set; }
    }

    public interface IHybridCommandInterpreter
    {
        Task<HybridCommandResult> InterpretAsync(string input, IList<WorkTask> candidates, string candidateContext, string model, CancellationToken cancellationToken);
        Task<HybridCommandResult> InterpretParsedAsync(string input, AgentDryRunResult parserResult, IList<WorkTask> candidates, string candidateContext, string model, CancellationToken cancellationToken);
    }

    public sealed class HybridCommandInterpreter : IHybridCommandInterpreter
    {
        private readonly AgentConversationService parser;
        private readonly ILocalLlmService llm;

        public HybridCommandInterpreter(AgentConversationService parser, ILocalLlmService llm) { this.parser = parser; this.llm = llm; }

        public async Task<HybridCommandResult> InterpretAsync(string input, IList<WorkTask> candidates, string candidateContext, string model, CancellationToken cancellationToken)
        {
            Stopwatch total = Stopwatch.StartNew(); Stopwatch parserWatch = Stopwatch.StartNew();
            AgentDryRunResult parsed = await parser.AnalyzeDryRunAsync(input, candidates); parserWatch.Stop();
            HybridCommandResult result = await InterpretParsedCoreAsync(input, parsed, candidates, candidateContext, model, cancellationToken, total);
            result.ParserLatencyMs = parserWatch.ElapsedMilliseconds; return result;
        }

        public Task<HybridCommandResult> InterpretParsedAsync(string input, AgentDryRunResult parserResult, IList<WorkTask> candidates, string candidateContext, string model, CancellationToken cancellationToken)
        {
            return InterpretParsedCoreAsync(input, parserResult, candidates, candidateContext, model, cancellationToken, Stopwatch.StartNew());
        }

        private async Task<HybridCommandResult> InterpretParsedCoreAsync(string input, AgentDryRunResult parsed, IList<WorkTask> candidates, string candidateContext, string model, CancellationToken cancellationToken, Stopwatch total)
        {
            HybridCommandResult result = new HybridCommandResult { ParserResult = parsed };
            string fallbackReason = GetFallbackReason(input, parsed);
            if (fallbackReason == null) return Finish(result, total, HybridCommandSource.PARSER, "Parser result is complete and consistent", FromParser(parsed), false);

            result.FallbackCalled = true;
            LocalAiAvailability availability = await llm.CheckAvailabilityAsync(model, cancellationToken);
            if (!availability.Online) { result.LlmResponse = new LocalLlmResponse { Model = model, Success = false, Error = availability.Error, FailureKind = availability.FailureKind }; return Finish(result, total, HybridCommandSource.UNRESOLVED, fallbackReason + "; Local AI unavailable: " + availability.FailureKind, null, false); }
            result.LlmResponse = await llm.ParseAsync(new LocalLlmRequest { Input = input, CandidateContext = candidateContext, Model = model }, cancellationToken);
            string validationError;
            if (!ValidateLlm(result.LlmResponse, candidates == null ? 0 : candidates.Count, out validationError))
                return Finish(result, total, HybridCommandSource.UNRESOLVED, fallbackReason + "; LLM rejected: " + validationError, null, false);

            LlmTaskParseResult candidate = result.LlmResponse.Result;
            if (IsSafeCorrection(input, parsed, candidate, fallbackReason))
                return Finish(result, total, HybridCommandSource.LLM, fallbackReason + "; validated LLM result resolves the conflict", candidate, false);

            if (Equivalent(parsed, candidate))
                return Finish(result, total, HybridCommandSource.PARSER, fallbackReason + "; LLM agrees with parser", FromParser(parsed), false);

            return Finish(result, total, HybridCommandSource.UNRESOLVED, fallbackReason + "; parser and LLM conflict without sufficient evidence", null, false);
        }

        private static HybridCommandResult Finish(HybridCommandResult value, Stopwatch watch, HybridCommandSource source, string reason, LlmTaskParseResult final, bool multi)
        {
            watch.Stop(); value.Source = source; value.Reason = reason; value.Result = final; value.MultiActionSchemaLimit = multi; value.NeedsClarification = source == HybridCommandSource.UNRESOLVED; value.HybridLatencyMs = watch.ElapsedMilliseconds;
            Debug.WriteLine("[Hybrid] Parser=" + (value.ParserResult == null ? "null" : value.ParserResult.Display) + " | FallbackToLLM=" + value.FallbackCalled + " | LLM=" + (value.LlmResponse == null || value.LlmResponse.Result == null ? "not-called/rejected" : value.LlmResponse.Result.Intent + " [" + string.Join(",", value.LlmResponse.Result.TargetIndexes) + "] " + (value.LlmResponse.Result.Status ?? "null")) + " | Final=" + source + " | Reason=" + reason);
            return value;
        }

        public static string GetFallbackReason(string input, AgentDryRunResult parsed)
        {
            if (parsed == null || parsed.Intent == "UNKNOWN") return "Parser intent is UNKNOWN";
            bool update = HasUpdateEvidence(input), reference = HasReferenceEvidence(input);
            if (update && parsed.Intent != "UPDATE_TASK") return "Explicit UPDATE expression conflicts with parser " + parsed.Intent + " result";
            if (parsed.Intent == "UPDATE_TASK" && !HasTarget(parsed)) return "Parser UPDATE target resolution failed";
            if (reference && (parsed.Reference == null || parsed.Intent == "UNKNOWN" || (parsed.Intent == "READ_TASK" && update))) return "Reference expression was not resolved consistently";
            return null;
        }

        private static bool ValidateLlm(LocalLlmResponse response, int candidateCount, out string error)
        {
            if (response == null || !response.JsonParseSuccess) { error = "JSON schema invalid"; return false; }
            if (!response.SchemaSuccess || response.Result == null) { error = response == null ? "No response" : (response.SchemaError ?? "Schema invalid"); return false; }
            LlmTaskParseResult r = response.Result;
            if (r.Reference != null && r.Reference != "LAST_CREATED" && r.Reference != "LAST_MENTIONED") { error = "reference is outside the schema enum"; return false; }
            if ((r.TargetIndexes ?? new List<int>()).Any(x => x < 1 || x > candidateCount)) { error = "target index outside candidate range"; return false; }
            if (r.Intent == "UPDATE_TASK" && string.IsNullOrWhiteSpace(r.Status)) { error = "UPDATE status is missing"; return false; }
            if (r.Intent == "UPDATE_TASK" && (r.TargetIndexes == null || r.TargetIndexes.Count == 0) && string.IsNullOrWhiteSpace(r.TargetKeyword) && string.IsNullOrWhiteSpace(r.Reference)) { error = "UPDATE target is missing"; return false; }
            if (r.Intent == "UPDATE_TASKS") { if (r.Actions == null || r.Actions.Count < 2) { error = "UPDATE_TASKS actions are missing"; return false; } foreach (LlmStatusAction action in r.Actions) { if (string.IsNullOrWhiteSpace(action.Status) || ((action.TargetIndexes == null || action.TargetIndexes.Count == 0) && string.IsNullOrWhiteSpace(action.TargetKeyword))) { error = "UPDATE_TASKS action is incomplete"; return false; } if ((action.TargetIndexes ?? new List<int>()).Any(x => x < 1 || x > candidateCount)) { error = "multi-action target index outside candidate range"; return false; } } }
            if (r.Intent == "CREATE_TASK" && string.IsNullOrWhiteSpace(r.Title)) { error = "CREATE title is missing"; return false; }
            error = null; return true;
        }

        private static bool IsSafeCorrection(string input, AgentDryRunResult parser, LlmTaskParseResult llm, string reason)
        {
            if (llm.Intent == "UNKNOWN") return false;
            if (reason.IndexOf("UPDATE", StringComparison.Ordinal) >= 0 || reason.IndexOf("Reference", StringComparison.Ordinal) >= 0)
                return (llm.Intent == "UPDATE_TASK" && HasTarget(llm) || llm.Intent == "UPDATE_TASKS" && llm.Actions != null && llm.Actions.Count > 1) && HasUpdateEvidence(input);
            return parser == null || parser.Intent == "UNKNOWN";
        }

        private static bool HasTarget(AgentDryRunResult r) { return r != null && ((r.TargetIndexes != null && r.TargetIndexes.Count > 0) || !string.IsNullOrWhiteSpace(r.TargetKeyword) || !string.IsNullOrWhiteSpace(r.Reference)); }
        private static bool HasTarget(LlmTaskParseResult r) { return r != null && ((r.TargetIndexes != null && r.TargetIndexes.Count > 0) || !string.IsNullOrWhiteSpace(r.TargetKeyword) || !string.IsNullOrWhiteSpace(r.Reference)); }
        private static bool HasUpdateEvidence(string value)
        {
            string text = value ?? "";
            if (Regex.IsMatch(text, "안\\s*끝난.*(보여|알려|뭐)|아직.*(업무|일).*(보여|알려|뭐)", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(text, "(완료한|완료된|끝난)\\s*(업무|것|거)?.*(보여|알려|뭐|조회|찾아)", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(text, "(완료한|완료된|끝낸|끝난)\\s*(업무|것|거)(은|는|이|가)?[? ]*$", RegexOptions.IgnoreCase)) return false;
            return Regex.IsMatch(text, "Done|완료|끝났|끝난\\s*(걸로|것으로)|하는 중(이야|으로)|진행중으로|바꿔|해놓|¿Ï·á|ÁøÇà", RegexOptions.IgnoreCase);
        }
        private static bool HasReferenceEvidence(string value) { return Regex.IsMatch(value ?? "", "방금|아까|그거|만든 거|등록한 거|¹æ±Ý|¾Æ±î", RegexOptions.IgnoreCase); }
        private static bool Equivalent(AgentDryRunResult a, LlmTaskParseResult b) { return a != null && b != null && a.Intent == b.Intent && a.Status == b.Status && (a.TargetIndexes ?? new List<int>()).OrderBy(x => x).SequenceEqual((b.TargetIndexes ?? new List<int>()).OrderBy(x => x)); }
        private static LlmTaskParseResult FromParser(AgentDryRunResult p) { return new LlmTaskParseResult { Intent = p.Intent, TargetIndexes = p.TargetIndexes, TargetKeyword = p.TargetKeyword, Reference = p.Reference, Status = p.Status, Title = p.Title, DueDate = p.DueDate }; }

        public static string Save(string runId, int no, string input, HybridCommandResult result)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit", "Task021"); Directory.CreateDirectory(dir); string path = Path.Combine(dir, "hybrid-results.jsonl");
            File.AppendAllText(path, JsonConvert.SerializeObject(new { recordedAt = DateTime.Now, runId, no, input, result }, Formatting.None) + Environment.NewLine); return path;
        }
    }
}
