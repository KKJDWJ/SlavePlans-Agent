using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlaveSplit.Services.AI
{
    public sealed class CompanyAiProvider : IAiProvider, IDisposable
    {
        private readonly HttpClient httpClient; private readonly AiProviderOptions options; private readonly ICredentialProvider credentialProvider; private AiProviderStatus status = AiProviderStatus.Unknown;
        public CompanyAiProvider(AiProviderOptions options, ICredentialProvider credentialProvider) { this.options = options; this.credentialProvider = credentialProvider; httpClient = new HttpClient(); }
        public string Name { get { return "Company AI"; } } public AiProviderStatus Status { get { return status; } } public event EventHandler StatusChanged;
        public async Task<AiResponse> SendAsync(AiRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(options.Endpoint)) return Error("Company AI Endpoint is required.", 0); Stopwatch watch = Stopwatch.StartNew();
            try { using (HttpRequestMessage message = BuildRequest(request)) using (HttpResponseMessage response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken)) { string body = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) { SetStatus(AiProviderStatus.Error); return Error("Company AI request failed with HTTP " + (int)response.StatusCode + ".", watch.ElapsedMilliseconds); } AiResponse parsed = ParseResponse(body, watch); SetStatus(parsed.Success ? AiProviderStatus.Connected : AiProviderStatus.Error); return parsed; } }
            catch (OperationCanceledException) { SetStatus(AiProviderStatus.Disconnected); return Error("Company AI request timed out.", watch.ElapsedMilliseconds); }
            catch (Exception ex) { SetStatus(AiProviderStatus.Error); return Error("Company AI request failed: " + ex.Message, watch.ElapsedMilliseconds); }
        }
        public async Task<AiResponse> TestConnectionAsync(CancellationToken cancellationToken) { return await SendAsync(new AiRequest { SystemPrompt = "Return exactly {\"type\":\"message\",\"content\":\"ok\"}.", UserMessage = "ping", ConversationMessages = new List<AiConversationMessage>(), TaskContext = new List<AiTaskContext>(), CurrentDateTime = DateTime.Now }, cancellationToken); }
        private HttpRequestMessage BuildRequest(AiRequest request)
        {
            List<object> messages = new List<object>(); messages.Add(new { role = "system", content = BuildSystemPrompt(request) }); foreach (AiConversationMessage item in request.ConversationMessages ?? new List<AiConversationMessage>()) messages.Add(new { role = item.Role == "assistant" ? "assistant" : "user", content = item.Content ?? "" }); messages.Add(new { role = "user", content = request.UserMessage ?? "" });
            object payload = new { model = options.Model, messages = messages, temperature = 0.2, response_format = new { type = "json_object" } }; HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, options.Endpoint.Trim()); string token = GetCredential(); if (!string.IsNullOrWhiteSpace(token)) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim()); message.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"); return message;
        }
        private static string BuildSystemPrompt(AiRequest request) { StringBuilder text = new StringBuilder(request.SystemPrompt ?? ""); text.Append("\nCurrent date/time: ").Append(request.CurrentDateTime.ToString("O")); if (request.TaskContext != null && request.TaskContext.Count > 0) { text.Append("\nKnown task references (use for entity resolution only; use read tools for factual answers):"); foreach (AiTaskContext task in request.TaskContext) text.Append("\n- ").Append(task.Id).Append(" | ").Append(task.Title).Append(" | ").Append(task.Status); } return text.ToString(); }
        private AiResponse ParseResponse(string content, Stopwatch watch)
        {
            try { JObject root = JObject.Parse(content); string value = (string)root.SelectToken("choices[0].message.content") ?? (string)root["content"] ?? (string)root["response"]; if (string.IsNullOrWhiteSpace(value)) return Error("Company AI returned an invalid response.", watch.ElapsedMilliseconds); return new AiResponse { Success = true, Content = value.Trim(), RawContent = null, ProviderName = Name, ModelName = options.Model, ElapsedMilliseconds = watch.ElapsedMilliseconds }; } catch { return Error("Company AI returned invalid JSON.", watch.ElapsedMilliseconds); }
        }
        private AiResponse Error(string value, long elapsed) { return new AiResponse { Success = false, ErrorMessage = value, ProviderName = Name, ModelName = options.Model, ElapsedMilliseconds = elapsed }; }
        private string GetCredential() { return credentialProvider == null ? null : credentialProvider.GetToken(); }
        private void SetStatus(AiProviderStatus value) { if (status == value) return; status = value; EventHandler handler = StatusChanged; if (handler != null) handler(this, EventArgs.Empty); }
        public void Dispose() { httpClient.Dispose(); }
    }
}
