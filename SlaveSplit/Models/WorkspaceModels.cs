using System;
using System.Collections.Generic;

namespace SlaveSplit.Models
{
    public sealed class GitHubCredentialProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public sealed class GitHubWorkspace
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Owner { get; set; }
        public string Repository { get; set; }
        public string CredentialProfileId { get; set; }
        public string ProjectId { get; set; }
        public bool IsEnabled { get; set; }
        public override string ToString() { return DisplayName ?? Repository ?? Id; }
    }

    public sealed class WorkspaceSettings
    {
        public string ActiveWorkspaceId { get; set; }
        public IList<GitHubWorkspace> Workspaces { get; set; } = new List<GitHubWorkspace>();
        public IList<GitHubCredentialProfile> CredentialProfiles { get; set; } = new List<GitHubCredentialProfile>();
    }
}
