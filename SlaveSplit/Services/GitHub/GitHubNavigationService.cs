using System;
namespace SlaveSplit.Services.GitHub
{
    public sealed class GitHubNavigationService
    {
        public event EventHandler<GitHubIssueNavigationEventArgs> IssueRequested; public event EventHandler BackRequested; public event EventHandler SettingsRequested;
        public void OpenIssue(int number) { EventHandler<GitHubIssueNavigationEventArgs> handler = IssueRequested; if (handler != null) handler(this, new GitHubIssueNavigationEventArgs(number)); } public void Back() { EventHandler handler = BackRequested; if (handler != null) handler(this, EventArgs.Empty); } public void OpenSettings() { EventHandler handler = SettingsRequested; if (handler != null) handler(this, EventArgs.Empty); }
    }
    public sealed class GitHubIssueNavigationEventArgs : EventArgs { public GitHubIssueNavigationEventArgs(int number) { Number = number; } public int Number { get; private set; } }
}
