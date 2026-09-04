using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SlaveSplit.Agents;

namespace SlaveSplit.Services.LocalLlm
{
    public sealed class ParserComparisonRecord
    {
        public DateTime RecordedAt { get; set; } public string RunId { get; set; } public string PromptVersion { get; set; } public int No { get; set; } public string Input { get; set; }
        public string ExpectedIntent { get; set; } public IList<int> ExpectedTargets { get; set; } public string ExpectedKeyword { get; set; } public string ExpectedReference { get; set; } public string ExpectedTitle { get; set; } public string ExpectedStatus { get; set; }
        public AgentDryRunResult CurrentParserResult { get; set; } public bool CurrentParserPass { get; set; } public bool BenchmarkPathMatch { get; set; }
        public string GraniteRawResponse { get; set; } public LlmTaskParseResult GraniteResult { get; set; }
        public bool JsonPass { get; set; } public bool SchemaPass { get; set; } public bool SemanticPass { get; set; } public bool EvaluationPass { get; set; } public bool FinalPass { get; set; } public string FailureKind { get; set; }
        public LocalLlmResponse Metrics { get; set; }
        public HybridCommandResult HybridResult { get; set; } public bool HybridPass { get; set; }
    }

    public static class ParserComparisonService
    {
        public static bool Matches(string intent, IList<int> targets, string status, string keyword, string reference, string title, AgentDryRunResult actual) { return actual != null && Equal(intent, targets, status, actual.Intent, actual.TargetIndexes, actual.Status); }
        public static bool Matches(string intent, IList<int> targets, string status, string keyword, string reference, string title, LlmTaskParseResult actual) { return actual != null && Equal(intent, targets, status, actual.Intent, actual.TargetIndexes, actual.Status); }
        private static bool Equal(string expectedIntent, IList<int> expectedTargets, string expectedStatus, string actualIntent, IList<int> actualTargets, string actualStatus)
        {
            if (!string.Equals(expectedIntent, actualIntent, StringComparison.Ordinal)) return false; if (!string.Equals(expectedStatus, actualStatus, StringComparison.Ordinal)) return false;
            int[] expected = (expectedTargets ?? new List<int>()).Distinct().OrderBy(x => x).ToArray(); int[] actual = (actualTargets ?? new List<int>()).Distinct().OrderBy(x => x).ToArray(); return expected.SequenceEqual(actual);
        }
        public static string Save(ParserComparisonRecord record) { string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SlaveSplit", "Task020"); Directory.CreateDirectory(dir); string safeRun = string.IsNullOrWhiteSpace(record.RunId) ? "adhoc" : record.RunId; string path = Path.Combine(dir, "comparison-" + safeRun + ".jsonl"); File.AppendAllText(path, JsonConvert.SerializeObject(record, Formatting.None) + Environment.NewLine); return path; }
    }
}
