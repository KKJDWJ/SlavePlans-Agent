using System.Collections.Generic;
namespace SlaveSplit.Services.GitHub
{
    public sealed class GitHubProjectInfo { public GitHubProjectInfo() { StatusFields = new List<GitHubProjectStatusField>(); } public string Id { get; set; } public string Title { get; set; } public IList<GitHubProjectStatusField> StatusFields { get; set; } public override string ToString() { return Title; } }
    public sealed class GitHubProjectStatusField { public GitHubProjectStatusField() { Options = new List<GitHubProjectStatusOption>(); } public string Id { get; set; } public string Name { get; set; } public IList<GitHubProjectStatusOption> Options { get; set; } public override string ToString() { return Name; } }
    public sealed class GitHubProjectStatusOption { public string Id { get; set; } public string Name { get; set; } public override string ToString() { return Name; } }
    public sealed class GitHubProjectsResult { public GitHubProjectsResult() { Projects = new List<GitHubProjectInfo>(); } public bool Success { get; set; } public string Message { get; set; } public IList<GitHubProjectInfo> Projects { get; set; } }
    public sealed class GitHubProjectItemResult { public bool Success { get; set; } public string Message { get; set; } public string ItemId { get; set; } }
}
