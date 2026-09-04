using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SlaveSplit.Services.LocalLlm
{
    public sealed class GraniteOllamaService : ILocalLlmService
    {
        private readonly HttpClient client; private readonly string endpoint;
        public GraniteOllamaService(string baseAddress = "http://localhost:11434", TimeSpan? timeout = null)
        {
            endpoint = baseAddress.TrimEnd('/'); client = new HttpClient { BaseAddress = new Uri(endpoint + "/"), Timeout = timeout ?? TimeSpan.FromSeconds(10) };
        }

        public async Task<LocalAiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken)
        {
            LocalAiAvailability result = new LocalAiAvailability { Endpoint = endpoint, Model = model };
            try { string[] models = await GetModelsAsync(cancellationToken); result.OllamaConnected = true; result.Online = models.Any(x => string.Equals(x, model, StringComparison.OrdinalIgnoreCase)); if (!result.Online) { result.FailureKind = LocalAiFailureKind.ModelNotFound; result.Error = "Configured model is not installed."; } }
            catch (TaskCanceledException ex) { result.FailureKind = LocalAiFailureKind.Timeout; result.Error = ex.Message; }
            catch (HttpRequestException ex) { result.FailureKind = LocalAiFailureKind.OllamaOffline; result.Error = ex.Message; }
            catch (Exception ex) { result.FailureKind = LocalAiFailureKind.RequestError; result.Error = ex.Message; }
            return result;
        }

        public async Task<string[]> GetModelsAsync(CancellationToken cancellationToken)
        {
            using (HttpResponseMessage response = await client.GetAsync("api/tags", cancellationToken))
            {
                string body = await response.Content.ReadAsStringAsync(); response.EnsureSuccessStatusCode();
                JArray models = JObject.Parse(body)["models"] as JArray;
                return models == null ? new string[0] : models.Select(x => (string)x["name"]).Where(x => !string.IsNullOrWhiteSpace(x)).OrderByDescending(x => x.IndexOf("granite", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            }
        }

        public async Task<LocalLlmResponse> ParseAsync(LocalLlmRequest request, CancellationToken cancellationToken)
        {
            LocalLlmResponse result = new LocalLlmResponse { StartTime = DateTime.Now, Model = request.Model, InputLength = (request.Input ?? "").Length };
            Stopwatch watch = Stopwatch.StartNew(); Process process = Process.GetCurrentProcess(); TimeSpan cpuStart = process.TotalProcessorTime;
            try
            {
                string prompt = BuildPrompt(request.Input, request.CandidateContext);
                JObject payload = new JObject { ["model"] = request.Model, ["system"] = SystemPrompt, ["prompt"] = prompt, ["stream"] = false, ["format"] = OutputSchema(), ["options"] = new JObject { ["temperature"] = 0, ["top_p"] = 0.1, ["seed"] = 20 } };
                using (StringContent content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await client.PostAsync("api/generate", content, cancellationToken))
                {
                    string body = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode) { result.FailureKind = body.IndexOf("model", StringComparison.OrdinalIgnoreCase) >= 0 && body.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0 ? LocalAiFailureKind.ModelNotFound : LocalAiFailureKind.RequestError; throw new InvalidOperationException("Ollama HTTP " + (int)response.StatusCode + ": " + body); }
                    JObject envelope = JObject.Parse(body); result.Model = (string)envelope["model"] ?? request.Model; result.RawOutput = (string)envelope["response"] ?? ""; result.OutputLength = result.RawOutput.Length; result.ColdStart = ((long?)envelope["load_duration"] ?? 0) >= 500000000L;
                    try { JObject parsed = JObject.Parse(ExtractJson(result.RawOutput)); result.JsonParseSuccess = true; result.Result = ParseStrict(parsed, out string schemaError); result.SchemaError = schemaError; result.SchemaSuccess = result.Result != null; }
                    catch (Exception ex) { result.Error = "JSON parse failed: " + ex.Message; result.FailureKind = LocalAiFailureKind.SchemaError; }
                    result.Success = result.SchemaSuccess;
                    if (!result.SchemaSuccess && string.IsNullOrWhiteSpace(result.FailureKind)) result.FailureKind = LocalAiFailureKind.SchemaError;
                }
                result.LatencyMs = watch.ElapsedMilliseconds; await ReadModelMemoryAsync(result, cancellationToken);
            }
            catch (TaskCanceledException ex) { result.Error = ex.Message; result.FailureKind = LocalAiFailureKind.Timeout; result.Success = false; }
            catch (HttpRequestException ex) { result.Error = ex.Message; result.FailureKind = LocalAiFailureKind.OllamaOffline; result.Success = false; }
            catch (Exception ex) { result.Error = ex.Message; if (string.IsNullOrWhiteSpace(result.FailureKind)) result.FailureKind = LocalAiFailureKind.RequestError; result.Success = false; }
            finally
            {
                watch.Stop(); process.Refresh(); result.EndTime = DateTime.Now; if (result.LatencyMs == 0) result.LatencyMs = watch.ElapsedMilliseconds; result.ProcessWorkingSetMB = Math.Round(process.WorkingSet64 / 1048576d, 1);
                double capacity = Math.Max(1, watch.Elapsed.TotalMilliseconds * Environment.ProcessorCount); result.CpuUsagePercent = Math.Round(Math.Min(100, (process.TotalProcessorTime - cpuStart).TotalMilliseconds / capacity * 100), 1); result.OllamaWorkingSetMB = Math.Round(Process.GetProcesses().Where(x => x.ProcessName.IndexOf("ollama", StringComparison.OrdinalIgnoreCase) >= 0 || x.ProcessName.IndexOf("llama", StringComparison.OrdinalIgnoreCase) >= 0).Sum(x => { try { return x.WorkingSet64; } catch { return 0L; } }) / 1048576d, 1);
            }
            return result;
        }

        private static string ExtractJson(string value) { int first = value.IndexOf('{'), last = value.LastIndexOf('}'); if (first < 0 || last < first) throw new FormatException("JSON object not found."); return value.Substring(first, last - first + 1); }
        private static LlmTaskParseResult ParseStrict(JObject json, out string error)
        {
            error = null; JToken targets = json["targetIndexes"]; if (targets == null || targets.Type != JTokenType.Array) { error = "targetIndexes must be int[]."; return null; }
            List<int> indexes = new List<int>(); foreach (JToken item in (JArray)targets) { if (item.Type != JTokenType.Integer) { error = "targetIndexes contains a non-integer or nested value."; return null; } indexes.Add((int)item); }
            string intent = (string)json["intent"]; string[] intents = { "CREATE_TASK", "READ_TASK", "UPDATE_TASK", "UPDATE_TASKS", "DELETE_TASK", "UNKNOWN" }; if (!intents.Contains(intent)) { error = "intent is outside the schema enum."; return null; }
            string status = json["status"] == null || json["status"].Type == JTokenType.Null ? null : (string)json["status"]; string[] statuses = { "Todo", "InProgress", "Blocked", "Done" }; if (status != null && !statuses.Contains(status)) { error = "status is outside the schema enum."; return null; }
            List<LlmStatusAction> actions = new List<LlmStatusAction>(); JArray actionArray = json["actions"] as JArray; if (actionArray != null) foreach (JObject action in actionArray.OfType<JObject>()) { JArray actionTargets = action["targetIndexes"] as JArray; string actionStatus = NullString(action["status"]); if (actionTargets == null || actionTargets.Any(x => x.Type != JTokenType.Integer) || !statuses.Contains(actionStatus)) { error = "actions contains an invalid target or status."; return null; } actions.Add(new LlmStatusAction { TargetIndexes = actionTargets.Select(x => (int)x).ToList(), TargetKeyword = NullString(action["targetKeyword"]), Status = actionStatus }); }
            return new LlmTaskParseResult { Intent = intent, TargetIndexes = indexes, TargetKeyword = NullString(json["targetKeyword"]), Reference = NullString(json["reference"]), Status = status, Date = NullString(json["date"]), Title = NullString(json["title"]), Actions = actions };
        }
        private static string NullString(JToken token) { return token == null || token.Type == JTokenType.Null ? null : token.Type == JTokenType.String ? (string)token : token.ToString(Formatting.None); }
        private async Task ReadModelMemoryAsync(LocalLlmResponse result, CancellationToken token)
        {
            try { using (HttpResponseMessage response = await client.GetAsync("api/ps", token)) { JObject root = JObject.Parse(await response.Content.ReadAsStringAsync()); JObject model = (root["models"] as JArray)?.OfType<JObject>().FirstOrDefault(x => string.Equals((string)x["name"], result.Model, StringComparison.OrdinalIgnoreCase)); if (model != null) { result.ModelMemoryMB = Math.Round(((long?)model["size"] ?? 0) / 1048576d, 1); result.ModelVramMB = Math.Round(((long?)model["size_vram"] ?? 0) / 1048576d, 1); } } } catch { }
        }
        private const string SystemPrompt = @"너는 Personal Work Agent의 한국어 자연어 명령 분석기다. 사용자의 문장을 실행하지 말고 구조화된 업무 명령으로만 변환한다. 업무를 생성하거나 변경하지 않는다. 설명하지 않는다. JSON만 반환한다.
사용자가 말하지 않은 값은 추론하거나 만들지 말고 null 또는 빈 배열로 둔다.
intent 규칙: 등록/추가/만들어=CREATE_TASK, 보여줘/알려줘/뭐야/뭐 남았어=READ_TASK, 바꿔/변경/완료/끝났어/진행중으로=UPDATE_TASK, 삭제/지워=DELETE_TASK.
서로 다른 업무에 서로 다른 상태가 지정되면 intent=UPDATE_TASKS이고 actions에 업무별 targetIndexes 또는 targetKeyword와 status를 넣는다. UPDATE_TASKS는 상태 변경만 허용한다.
targetIndexes에는 사용자가 직접 말한 번호만 정수로 넣는다. Candidate에서 제목을 찾았다는 이유로 번호를 넣지 않는다. '2번만'=[2], '2번하고 3번'=[2,3], 번호 언급 없음=[].
targetKeyword에는 사용자가 말한 업무 핵심어만 넣는다. FMP 업무/관련 업무는 FMP이다.
reference 규칙: 방금 만든/등록한 것 또는 아까 등록한 것=LAST_CREATED. 번호나 키워드가 있으면 불필요한 reference를 만들지 않는다.
status 규칙: 끝났어/끝난 걸로/완료/다 했어/Done=Done, 하고 있어/하는 중/진행중=InProgress, 해야 해/할 일/남은 업무=Todo. 상태 표현이 없으면 null이다.
CREATE_TASK일 때만 사용자가 말한 업무 제목을 title에 넣는다. date는 명시된 날짜 표현이 없으면 null이다.
예시1 입력: 2번만 Done으로 바꿔줘
예시1 출력: {""intent"":""UPDATE_TASK"",""targetIndexes"":[2],""targetKeyword"":null,""reference"":null,""status"":""Done"",""date"":null,""title"":null}
예시2 입력: FMP 관련해서 아직 안 끝난 업무 보여줘
예시2 출력: {""intent"":""READ_TASK"",""targetIndexes"":[],""targetKeyword"":""FMP"",""reference"":null,""status"":null,""date"":null,""title"":null}
예시3 입력: 아까 하던 FMP 그거 이제 끝난 걸로 해놓자
예시3 출력: {""intent"":""UPDATE_TASK"",""targetIndexes"":[],""targetKeyword"":""FMP"",""reference"":null,""status"":""Done"",""date"":null,""title"":null}
예시4 입력: 오늘 해야 하는 거 뭐 남았냐
예시4 출력: {""intent"":""READ_TASK"",""targetIndexes"":[],""targetKeyword"":null,""reference"":null,""status"":""Todo"",""date"":""오늘"",""title"":null}
예시5 입력: 1번은 완료하고 2번은 진행중으로 해줘
예시5 출력: {""intent"":""UPDATE_TASKS"",""targetIndexes"":[],""targetKeyword"":null,""reference"":null,""status"":null,""date"":null,""title"":null,""actions"":[{""targetIndexes"":[1],""targetKeyword"":null,""status"":""Done""},{""targetIndexes"":[2],""targetKeyword"":null,""status"":""InProgress""}]}";
        private static string BuildPrompt(string input, string context) { return "Candidate Context (참고만 하며 번호를 추론하지 말 것):\n" + MinimizeContext(context) + "\n\nInput:\n" + (input ?? ""); }
        private static string MinimizeContext(string context) { if (string.IsNullOrWhiteSpace(context)) return "(없음)"; return string.Join("\n", context.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Take(10).Select(x => x.Trim())); }
        private static JObject OutputSchema()
        {
            return JObject.Parse(@"{""type"":""object"",""additionalProperties"":false,""properties"":{""intent"":{""type"":""string"",""enum"":[""CREATE_TASK"",""READ_TASK"",""UPDATE_TASK"",""UPDATE_TASKS"",""DELETE_TASK"",""UNKNOWN""]},""targetIndexes"":{""type"":""array"",""items"":{""type"":""integer""}},""targetKeyword"":{""type"":[""string"",""null""]},""reference"":{""type"":[""string"",""null""],""enum"":[""LAST_CREATED"",""LAST_MENTIONED"",null]},""status"":{""type"":[""string"",""null""],""enum"":[""Todo"",""InProgress"",""Blocked"",""Done"",null]},""date"":{""type"":[""string"",""null""]},""title"":{""type"":[""string"",""null""]},""actions"":{""type"":""array"",""items"":{""type"":""object"",""additionalProperties"":false,""properties"":{""targetIndexes"":{""type"":""array"",""items"":{""type"":""integer""}},""targetKeyword"":{""type"":[""string"",""null""]},""status"":{""type"":""string"",""enum"":[""Todo"",""InProgress"",""Blocked"",""Done""]}},""required"":[""targetIndexes"",""targetKeyword"",""status""]}}},""required"":[""intent"",""targetIndexes"",""targetKeyword"",""reference"",""status"",""date"",""title""]}");
        }
    }
}
