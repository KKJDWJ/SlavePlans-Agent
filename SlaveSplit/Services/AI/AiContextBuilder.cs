using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;
namespace SlaveSplit.Services.AI
{
    public interface IAiContextBuilder { Task<AiRequest> BuildAsync(AgentRequest request); }
    public sealed class AiContextBuilder : IAiContextBuilder
    {
        private readonly TaskService taskService; private readonly IClock clock; private readonly AiProviderOptions options; private readonly string systemPrompt;
        public AiContextBuilder(TaskService taskService, IClock clock, AiProviderOptions options, string systemPrompt) { this.taskService = taskService; this.clock = clock; this.options = options; this.systemPrompt = systemPrompt; }
        public async Task<AiRequest> BuildAsync(AgentRequest request)
        {
            var tasks = await taskService.GetAllAsync(); var relevant = tasks.Where(x => x.Status != WorkTaskStatus.Done || (x.CompletedAt.HasValue && x.CompletedAt.Value.Date >= clock.Now.Date.AddDays(-1))).OrderByDescending(x => x.UpdatedAt).Take(30).Select(x => new AiTaskContext { Id = x.Id, Title = x.Title, Status = x.Status.ToString() }).ToList(); var messages = (request.ConversationMessages ?? new List<Message>()).OrderByDescending(x => x.CreatedAt).Take(options.ConversationContextLimit).OrderBy(x => x.CreatedAt).Select(x => new AiConversationMessage { Role = x.IsUser ? "user" : "assistant", Content = x.Content }).ToList(); return new AiRequest { SystemPrompt = systemPrompt, UserMessage = request.Message, ConversationMessages = messages, TaskContext = relevant, CurrentDateTime = clock.Now };
        }
    }
}
