using System.Threading.Tasks;
using SlaveSplit.Models;
namespace SlaveSplit.Agents { public interface IAgentService { Task<AgentResponse> ProcessAsync(AgentRequest request); void ResetContext(); } }
