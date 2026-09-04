using System;
using System.Threading;
using System.Threading.Tasks;
namespace SlaveSplit.Services.AI
{
    public interface IAiProvider
    {
        string Name { get; }
        AiProviderStatus Status { get; }
        event EventHandler StatusChanged;
        Task<AiResponse> SendAsync(AiRequest request, CancellationToken cancellationToken);
        Task<AiResponse> TestConnectionAsync(CancellationToken cancellationToken);
    }
}
