using System;
using System.Net;
using Newtonsoft.Json;
namespace SlaveSplit.Services.GitHub
{
    public enum GitHubConnectionStatus { NotConfigured, Connecting, Connected, AuthenticationFailed, RepositoryNotFound, Disconnected, Error }
    public sealed class GitHubSettings
    {
        public GitHubSettings() { Enabled = false; Owner = "KKJDWJ"; Repository = "SlaveSplit"; TimeoutSeconds = 15; }
        public bool Enabled { get; set; } public bool AutoCreateIssues { get; set; } public bool ProjectSyncEnabled { get; set; } public string Owner { get; set; } public string Repository { get; set; } public int TimeoutSeconds { get; set; } public string ProjectId { get; set; } public string ProjectTitle { get; set; } public string StatusFieldId { get; set; } public string TodoOptionId { get; set; } public string InProgressOptionId { get; set; } public string DoneOptionId { get; set; } public string BlockedOptionId { get; set; }
    }
    public sealed class GitHubRepositoryInfo
    {
        public long Id { get; set; } public string Name { get; set; } public string FullName { get; set; } public string Description { get; set; } public bool Private { get; set; } public string DefaultBranch { get; set; } public string HtmlUrl { get; set; }
        public string VisibilityText { get { return Private ? "Private Repository" : "Public Repository"; } }
    }
    public sealed class GitHubConnectionResult
    {
        public bool Success { get; set; } public HttpStatusCode? StatusCode { get; set; } public string Message { get; set; } public long ElapsedMilliseconds { get; set; } public GitHubRepositoryInfo Repository { get; set; } public GitHubConnectionStatus Status { get; set; }
    }
    internal sealed class GitHubRepositoryDto
    {
        [JsonProperty("id")] public long Id { get; set; } [JsonProperty("name")] public string Name { get; set; } [JsonProperty("full_name")] public string FullName { get; set; } [JsonProperty("description")] public string Description { get; set; } [JsonProperty("private")] public bool Private { get; set; } [JsonProperty("default_branch")] public string DefaultBranch { get; set; } [JsonProperty("html_url")] public string HtmlUrl { get; set; }
    }
    public sealed class GitHubStatusService
    {
        private GitHubConnectionStatus status = GitHubConnectionStatus.NotConfigured; public event EventHandler StatusChanged; public GitHubConnectionStatus Status { get { return status; } }
        public void SetStatus(GitHubConnectionStatus value) { if (status == value) return; status = value; EventHandler handler = StatusChanged; if (handler != null) handler(this, EventArgs.Empty); }
        public string HeaderText { get { return status == GitHubConnectionStatus.Connected ? "● GitHub" : status == GitHubConnectionStatus.Connecting ? "◌ GitHub" : "○ GitHub"; } }
    }
}
