using System.Threading;
using System.Threading.Tasks;

namespace SlaveSplit.Services.LocalLlm
{
    public interface ILocalLlmService
    {
        Task<string[]> GetModelsAsync(CancellationToken cancellationToken);
        Task<LocalAiAvailability> CheckAvailabilityAsync(string model, CancellationToken cancellationToken);
        Task<LocalLlmResponse> ParseAsync(LocalLlmRequest request, CancellationToken cancellationToken);
    }
}
