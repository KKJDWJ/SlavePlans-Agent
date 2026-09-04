using System;

namespace SlaveSplit.Services.LocalLlm
{
    public sealed class LocalLlmSettings
    {
        private static string runtimeModel;
        public bool Enabled { get; set; }
        public string Provider { get; set; }
        public string Model { get; set; }
        public string Endpoint { get; set; }
        public int TimeoutSeconds { get; set; }
        public string EffectiveModel { get { return string.IsNullOrWhiteSpace(runtimeModel) ? Model : runtimeModel; } }
        public static void SetRuntimeModel(string value) { if (!string.IsNullOrWhiteSpace(value)) runtimeModel = value; }

        public LocalLlmSettings() { Enabled = true; Provider = "Ollama"; Model = "qwen3:4b-instruct"; Endpoint = "http://localhost:11434"; TimeoutSeconds = 30; }
        public static LocalLlmSettings Load()
        {
            LocalLlmSettings value = new LocalLlmSettings(); string enabled = Environment.GetEnvironmentVariable("SLAVESPLIT_LOCAL_LLM_ENABLED"), model = Environment.GetEnvironmentVariable("SLAVESPLIT_LOCAL_LLM_MODEL"), endpoint = Environment.GetEnvironmentVariable("SLAVESPLIT_LOCAL_LLM_ENDPOINT"), timeout = Environment.GetEnvironmentVariable("SLAVESPLIT_LOCAL_LLM_TIMEOUT_SECONDS");
            bool parsedEnabled; int parsedTimeout; if (bool.TryParse(enabled, out parsedEnabled)) value.Enabled = parsedEnabled; if (!string.IsNullOrWhiteSpace(model)) value.Model = model; if (!string.IsNullOrWhiteSpace(endpoint)) value.Endpoint = endpoint; if (int.TryParse(timeout, out parsedTimeout)) value.TimeoutSeconds = Math.Max(1, parsedTimeout); return value;
        }
    }
}
