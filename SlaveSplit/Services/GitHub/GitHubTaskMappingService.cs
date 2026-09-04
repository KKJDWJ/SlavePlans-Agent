using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Models;

namespace SlaveSplit.Services.GitHub
{
    public interface IGitHubTaskMappingService
    {
        event EventHandler MappingChanged;
        Task<bool> IsTrackedAsync(int issueNumber);
        Task<WorkTask> GetTrackedTaskAsync(int issueNumber);
        Task<WorkTask> TrackIssueAsync(GitHubIssueInfo issue);
        Task<WorkTask> LinkExistingTaskAsync(GitHubIssueInfo issue, WorkTask task);
        Task<IReadOnlyList<WorkTask>> GetLinkCandidatesAsync();
        Task<IReadOnlyList<WorkTask>> GetTrackedTasksAsync();
    }

    public sealed class GitHubTaskMappingService : IGitHubTaskMappingService
    {
        private readonly TaskService tasks;
        public GitHubTaskMappingService(TaskService tasks) { this.tasks = tasks; }
        public event EventHandler MappingChanged;

        public async Task<bool> IsTrackedAsync(int issueNumber) { return await GetTrackedTaskAsync(issueNumber) != null; }

        public async Task<WorkTask> GetTrackedTaskAsync(int issueNumber)
        {
            string externalId = issueNumber.ToString();
            IReadOnlyList<WorkTask> all = await tasks.GetAllAsync();
            return all.FirstOrDefault(x => x.Source == TaskSource.GitHub && string.Equals(x.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<WorkTask> TrackIssueAsync(GitHubIssueInfo issue)
        {
            Validate(issue);
            WorkTask existing = await GetTrackedTaskAsync(issue.Number);
            if (existing != null) return existing;
            WorkTask task = await tasks.CreateTaskAsync(issue.Title, issue.Body, "GitHub", TaskPriority.Normal, null, false);
            task.Source = TaskSource.GitHub;
            task.ExternalId = issue.Number.ToString();
            task.ExternalUrl = issue.HtmlUrl;
            task.GitHubNodeId = issue.NodeId;
            await tasks.UpdateAsync(task, "Imported from GitHub Issue #" + issue.Number);
            if (issue.State == GitHubIssueState.Closed) await tasks.CompleteAsync(task, "Imported from closed GitHub Issue #" + issue.Number);
            RaiseChanged();
            return task;
        }

        public async Task<WorkTask> LinkExistingTaskAsync(GitHubIssueInfo issue, WorkTask task)
        {
            Validate(issue);
            if (task == null) throw new ArgumentNullException("task");
            WorkTask existing = await GetTrackedTaskAsync(issue.Number);
            if (existing != null) return existing;
            if (task.Source != TaskSource.Local || !string.IsNullOrWhiteSpace(task.ExternalId)) throw new InvalidOperationException("Only an unlinked local task can be linked.");
            task.Source = TaskSource.GitHub;
            task.ExternalId = issue.Number.ToString();
            task.ExternalUrl = issue.HtmlUrl;
            task.GitHubNodeId = issue.NodeId;
            await tasks.UpdateAsync(task, "Linked to GitHub Issue #" + issue.Number);
            RaiseChanged();
            return task;
        }

        public async Task<IReadOnlyList<WorkTask>> GetLinkCandidatesAsync()
        {
            IReadOnlyList<WorkTask> all = await tasks.GetAllAsync();
            return all.Where(x => x.Source == TaskSource.Local && string.IsNullOrWhiteSpace(x.ExternalId)).OrderByDescending(x => x.UpdatedAt).ToList();
        }

        public async Task<IReadOnlyList<WorkTask>> GetTrackedTasksAsync()
        {
            IReadOnlyList<WorkTask> all = await tasks.GetAllAsync();
            return all.Where(x => x.Source == TaskSource.GitHub && !string.IsNullOrWhiteSpace(x.ExternalId)).ToList();
        }

        private static void Validate(GitHubIssueInfo issue) { if (issue == null || issue.Number <= 0) throw new ArgumentException("A valid GitHub issue is required."); }
        private void RaiseChanged() { EventHandler handler = MappingChanged; if (handler != null) handler(this, EventArgs.Empty); }
    }
}
