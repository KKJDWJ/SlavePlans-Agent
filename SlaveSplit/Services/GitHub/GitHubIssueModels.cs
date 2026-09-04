using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
namespace SlaveSplit.Services.GitHub
{
    public enum GitHubIssueState { All, Open, Closed }
    public enum GitHubIssueSort { RecentlyUpdated, Newest, Oldest }
    public sealed class GitHubIssueQuery
    {
        public GitHubIssueQuery() { State = GitHubIssueState.Open; Page = 1; PerPage = 50; Sort = GitHubIssueSort.RecentlyUpdated; Direction = "desc"; }
        public GitHubIssueState State { get; set; } public int Page { get; set; } public int PerPage { get; set; } public GitHubIssueSort Sort { get; set; } public string Direction { get; set; } public string Labels { get; set; } public string Assignee { get; set; } public DateTime? Since { get; set; }
    }
    public sealed class GitHubLabelInfo { public string Name { get; set; } public string Description { get; set; } public string Color { get; set; } }
    public sealed class GitHubUserInfo { public string Login { get; set; } public string AvatarUrl { get; set; } }
    public sealed class GitHubIssueInfo
    {
        public int Number { get; set; } public string NodeId { get; set; } public string Title { get; set; } public string Body { get; set; } public GitHubIssueState State { get; set; } public string HtmlUrl { get; set; } public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public DateTime? ClosedAt { get; set; } public GitHubUserInfo Author { get; set; } public IList<GitHubUserInfo> Assignees { get; set; } public IList<GitHubLabelInfo> Labels { get; set; } public bool IsTracked { get; set; } public Guid? TrackedTaskId { get; set; }
        public GitHubIssueInfo() { Assignees = new List<GitHubUserInfo>(); Labels = new List<GitHubLabelInfo>(); }
        public string StateText { get { return (State == GitHubIssueState.Closed ? "CLOSED" : "OPEN") + (IsTracked ? " · TRACKED" : ""); } } public string NumberText { get { return "#" + Number; } } public string BodyText { get { return string.IsNullOrWhiteSpace(Body) ? "No description provided." : Body; } } public string AssigneeText { get { return Assignees.Count == 0 ? "Unassigned" : string.Join(", ", System.Linq.Enumerable.Select(Assignees, x => x.Login)); } } public string UpdatedText { get { return "Updated " + UpdatedAt.ToString("yyyy.MM.dd HH:mm"); } }
    }
    public sealed class GitHubIssuesResult
    {
        public bool Success { get; set; } public HttpStatusCode? StatusCode { get; set; } public string Message { get; set; } public long ElapsedMilliseconds { get; set; } public IList<GitHubIssueInfo> Issues { get; set; } public int Page { get; set; } public bool HasNextPage { get; set; } public GitHubConnectionStatus Status { get; set; }
        public GitHubIssuesResult() { Issues = new List<GitHubIssueInfo>(); }
    }
    public sealed class GitHubIssueResult { public bool Success { get; set; } public HttpStatusCode? StatusCode { get; set; } public string Message { get; set; } public long ElapsedMilliseconds { get; set; } public GitHubIssueInfo Issue { get; set; } public GitHubConnectionStatus Status { get; set; } }
    public sealed class GitHubCreateIssueRequest { public GitHubCreateIssueRequest() { Labels = new List<string>(); } [JsonProperty("title")] public string Title { get; set; } [JsonProperty("body")] public string Body { get; set; } [JsonProperty("labels")] public IList<string> Labels { get; set; } }
    internal sealed class GitHubIssueDto
    {
        [JsonProperty("number")] public int Number { get; set; } [JsonProperty("node_id")] public string NodeId { get; set; } [JsonProperty("title")] public string Title { get; set; } [JsonProperty("body")] public string Body { get; set; } [JsonProperty("state")] public string State { get; set; } [JsonProperty("html_url")] public string HtmlUrl { get; set; } [JsonProperty("created_at")] public DateTime CreatedAt { get; set; } [JsonProperty("updated_at")] public DateTime UpdatedAt { get; set; } [JsonProperty("closed_at")] public DateTime? ClosedAt { get; set; } [JsonProperty("user")] public GitHubUserDto User { get; set; } [JsonProperty("assignees")] public IList<GitHubUserDto> Assignees { get; set; } [JsonProperty("labels")] public IList<GitHubLabelDto> Labels { get; set; } [JsonProperty("pull_request")] public object PullRequest { get; set; }
    }
    internal sealed class GitHubUserDto { [JsonProperty("login")] public string Login { get; set; } [JsonProperty("avatar_url")] public string AvatarUrl { get; set; } }
    internal sealed class GitHubLabelDto { [JsonProperty("name")] public string Name { get; set; } [JsonProperty("description")] public string Description { get; set; } [JsonProperty("color")] public string Color { get; set; } }
}
