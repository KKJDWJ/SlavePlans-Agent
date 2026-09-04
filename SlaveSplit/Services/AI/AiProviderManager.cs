using System;
using System.Threading;
using System.Threading.Tasks;
namespace SlaveSplit.Services.AI
{
    public sealed class AiProviderManager
    {
        private readonly MockAiProvider mock; private readonly CompanyAiProvider company; public AiProviderOptions Options { get; private set; }
        public AiProviderManager(AiProviderOptions options, MockAiProvider mock, CompanyAiProvider company) { Options = options; this.mock = mock; this.company = company; mock.StatusChanged += ForwardStatus; company.StatusChanged += ForwardStatus; }
        public IAiProvider Current { get { return Options.Provider == AiProviderKind.Company ? (IAiProvider)company : mock; } }
        public event EventHandler StatusChanged;
        public void Select(AiProviderKind kind) { Options.Provider = kind; EventHandler handler = StatusChanged; if (handler != null) handler(this, EventArgs.Empty); }
        public Task<AiResponse> TestConnectionAsync(CancellationToken token) { return Current.TestConnectionAsync(token); }
        private void ForwardStatus(object sender, EventArgs e) { if (sender != Current) return; EventHandler handler = StatusChanged; if (handler != null) handler(this, EventArgs.Empty); }
    }
}
