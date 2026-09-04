using System;
using System.Collections.Generic;

namespace SlaveSplit.Services.LocalLlm
{
    public static class LocalAiFailureKind { public const string None = null, OllamaOffline = "OLLAMA_OFFLINE", ModelNotFound = "MODEL_NOT_FOUND", Disabled = "LOCAL_LLM_DISABLED", Timeout = "LLM_TIMEOUT", SchemaError = "LLM_SCHEMA_ERROR", RequestError = "LLM_REQUEST_ERROR"; }
    public sealed class LocalAiAvailability { public bool Online { get; set; } public bool OllamaConnected { get; set; } public string Endpoint { get; set; } public string Model { get; set; } public string FailureKind { get; set; } public string Error { get; set; } }
    public sealed class LlmTaskParseResult
    {
        public string Intent { get; set; }
        public IList<int> TargetIndexes { get; set; } = new List<int>();
        public string TargetKeyword { get; set; }
        public string Reference { get; set; }
        public string Status { get; set; }
        public string Date { get; set; }
        public string DueDate { get; set; }
        public string Title { get; set; }
        public IList<LlmStatusAction> Actions { get; set; } = new List<LlmStatusAction>();
    }
    public sealed class LlmStatusAction { public IList<int> TargetIndexes { get; set; } = new List<int>(); public string TargetKeyword { get; set; } public string Status { get; set; } }

    public sealed class LocalLlmRequest
    {
        public string Input { get; set; }
        public string CandidateContext { get; set; }
        public string Model { get; set; }
    }

    public sealed class LocalLlmResponse
    {
        public bool Success { get; set; }
        public bool JsonParseSuccess { get; set; }
        public bool SchemaSuccess { get; set; }
        public string SchemaError { get; set; }
        public bool ColdStart { get; set; }
        public string Model { get; set; }
        public string RawOutput { get; set; }
        public string Error { get; set; }
        public string FailureKind { get; set; }
        public LlmTaskParseResult Result { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long LatencyMs { get; set; }
        public int InputLength { get; set; }
        public int OutputLength { get; set; }
        public double ProcessWorkingSetMB { get; set; }
        public double CpuUsagePercent { get; set; }
        public double OllamaWorkingSetMB { get; set; }
        public double ModelMemoryMB { get; set; }
        public double ModelVramMB { get; set; }
    }
}
