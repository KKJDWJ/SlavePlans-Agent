using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace SlaveSplit.Services.AI
{
    public interface IAgentCommandParser { AgentCommandParseResult Parse(string content); AgentCommandBatchParseResult ParseBatch(string content); }
    public sealed class AgentCommandParser : IAgentCommandParser
    {
        public AgentCommandParseResult Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return Fail("AI response is empty."); try { int start = content.IndexOf('{'), end = content.LastIndexOf('}'); if (start < 0 || end <= start) return Fail("JSON object was not found."); JObject json = JObject.Parse(content.Substring(start, end - start + 1)); string actionText = ((string)json["action"] ?? "UNKNOWN").Replace("_", ""); AgentCommandAction action; if (!Enum.TryParse<AgentCommandAction>(actionText, true, out action)) action = AgentCommandAction.Unknown; double confidence = json["confidence"] == null ? 0 : (double)json["confidence"]; if (confidence < 0 || confidence > 1) return Fail("Confidence must be between 0 and 1."); Guid parsedId; Guid? id = Guid.TryParse((string)json["taskId"], out parsedId) ? parsedId : (Guid?)null; DateTime parsedDate; DateTime? due = DateTime.TryParse((string)json["dueDate"], out parsedDate) ? parsedDate : (DateTime?)null;
                AgentCommand command = new AgentCommand { Action = action, Confidence = confidence, TaskId = id, Title = (string)json["title"], Description = (string)json["description"], Status = (string)json["status"], Priority = (string)json["priority"], DueDate = due, BlockedReason = (string)json["blockedReason"], Memo = (string)json["memo"] }; if (action == AgentCommandAction.CreateTask && string.IsNullOrWhiteSpace(command.Title)) return Fail("CREATE_TASK requires a title."); return new AgentCommandParseResult { Success = true, Command = command };
            } catch (Exception ex) { return Fail("JSON parse failed: " + ex.Message); }
        }
        public AgentCommandBatchParseResult ParseBatch(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BatchFail("AI response is empty."); try { int start = content.IndexOf('{'), end = content.LastIndexOf('}'); if (start < 0 || end <= start) return BatchFail("JSON object was not found."); JObject root = JObject.Parse(content.Substring(start, end - start + 1)); JArray commands = root["commands"] as JArray; if (commands == null) { AgentCommandParseResult single = Parse(content); if (!single.Success) return BatchFail(single.ErrorMessage); return new AgentCommandBatchParseResult { Success = true, Batch = new AgentCommandBatch { Commands = new List<AgentCommand> { single.Command }, Confidence = single.Command.Confidence } }; } AgentCommandBatch batch = new AgentCommandBatch { Summary = (string)root["summary"], Confidence = root["confidence"] == null ? 1 : (double)root["confidence"], RequiresConfirmation = root["requiresConfirmation"] != null && (bool)root["requiresConfirmation"] }; foreach (JToken token in commands) { AgentCommandParseResult parsed = Parse(token.ToString()); if (!parsed.Success) return BatchFail(parsed.ErrorMessage); batch.Commands.Add(parsed.Command); } if (batch.Commands.Count == 0) return BatchFail("Command batch is empty."); return new AgentCommandBatchParseResult { Success = true, Batch = batch };
            } catch (Exception ex) { return BatchFail("Batch JSON parse failed: " + ex.Message); }
        }
        private static AgentCommandParseResult Fail(string message) { return new AgentCommandParseResult { Success = false, ErrorMessage = message }; }
        private static AgentCommandBatchParseResult BatchFail(string message) { return new AgentCommandBatchParseResult { Success = false, ErrorMessage = message }; }
    }
}
