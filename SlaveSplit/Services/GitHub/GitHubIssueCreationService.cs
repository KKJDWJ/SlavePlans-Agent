using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SlaveSplit.Models;

namespace SlaveSplit.Services.GitHub
{
    public sealed class GitHubTaskCreationResult
    {
        public bool Success { get; set; } public bool Skipped { get; set; } public string Message { get; set; } public WorkTask Task { get; set; } public GitHubIssueInfo Issue { get; set; } public int? StatusCode { get; set; } public string ErrorType { get; set; } public string SafeErrorMessage { get; set; }
    }

    public interface IGitHubIssueCreationService
    {
        bool IsAutoCreateEnabled { get; }
        Task<GitHubTaskCreationResult> CreateForTaskAsync(WorkTask task, CancellationToken cancellationToken);
    }

    public sealed class GitHubIssueCreationService : IGitHubIssueCreationService
    {
        private readonly IGitHubService github; private readonly GitHubSettingsService settingsService; private readonly IGitHubTaskMappingService mappings; private readonly HashSet<Guid> active = new HashSet<Guid>(); private readonly object sync = new object(); public IGitHubSyncService ProjectSyncService { private get; set; }
        public GitHubIssueCreationService(IGitHubService github, GitHubSettingsService settingsService, IGitHubTaskMappingService mappings) { this.github = github; this.settingsService = settingsService; this.mappings = mappings; }
        public bool IsAutoCreateEnabled { get { GitHubSettings value = settingsService.Load(); return value.Enabled && value.AutoCreateIssues; } }
        public async Task<GitHubTaskCreationResult> CreateForTaskAsync(WorkTask task, CancellationToken cancellationToken)
        {
            if (task == null) return Fail(task, "Local task was not found.");
            if (task.Source == TaskSource.GitHub || !string.IsNullOrWhiteSpace(task.ExternalId)) return new GitHubTaskCreationResult { Success = true, Skipped = true, Task = task, Message = "Already linked to GitHub Issue #" + task.ExternalId + "." };
            lock (sync) { if (active.Contains(task.Id)) return new GitHubTaskCreationResult { Success = false, Skipped = true, Task = task, Message = "GitHub Issue creation is already in progress." }; active.Add(task.Id); }
            try
            {
                GitHubSettings settings = settingsService.Load(); if (!settings.Enabled) return Fail(task, "GitHub is disabled. Local task is still available.");
                GitHubIssueResult result = await github.CreateIssueAsync(settings, new GitHubCreateIssueRequest { Title = task.Title, Body = task.Description ?? "" }, cancellationToken);
                if (!result.Success || result.Issue == null) return Fail(task, result.Message + " Local task is still available.", result.StatusCode.HasValue ? (int?)result.StatusCode.Value : null, result.Status.ToString());
                WorkTask mapped = await mappings.LinkExistingTaskAsync(result.Issue, task); if (ProjectSyncService != null) await ProjectSyncService.QueueProjectStatusAsync(mapped);
                return new GitHubTaskCreationResult { Success = true, Task = mapped, Issue = result.Issue, Message = "GitHub Issue #" + result.Issue.Number + " created." };
            }
            catch (Exception ex) { return Fail(task, "GitHub Issue creation failed. Local task is still available.", null, ex.GetType().Name); }
            finally { lock (sync) active.Remove(task.Id); }
        }
        private static GitHubTaskCreationResult Fail(WorkTask task, string message, int? statusCode = null, string errorType = null) { string safe = string.IsNullOrWhiteSpace(message) ? "GitHub Issue creation failed." : message; return new GitHubTaskCreationResult { Success = false, Task = task, Message = safe, StatusCode = statusCode, ErrorType = errorType, SafeErrorMessage = safe }; }
    }
}
