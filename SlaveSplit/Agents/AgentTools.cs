using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;
using SlaveSplit.Services.GitHub;

namespace SlaveSplit.Agents
{
    public sealed class AgentToolCall { public string Tool { get; set; } public IDictionary<string, string> Arguments { get; set; } public AgentToolCall() { Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); } }
    public sealed class AgentToolResult
    {
        public bool Success { get; set; } public string Tool { get; set; } public Guid? TaskId { get; set; } public string Title { get; set; } public string Message { get; set; }
        public int? StatusCode { get; set; } public string ErrorType { get; set; } public string SafeErrorMessage { get; set; }
        public string GitHubIssueNumber { get; set; } public bool GitHubCreated { get; set; } public bool ProjectAdded { get; set; } public GitHubSyncStatus? GitHubSyncStatus { get; set; } public GitHubSyncStatus? GitHubProjectSyncStatus { get; set; } public string GitHubSyncError { get; set; }
        public bool IncludeCreatedAt { get; set; } public bool IncludeDescription { get; set; } public string QueryLabel { get; set; } public string DateFallback { get; set; }
        public IList<WorkTask> Tasks { get; set; } public IList<TaskHistory> History { get; set; } public IList<GitHubIssueInfo> Issues { get; set; }
        public AgentToolResult() { Tasks = new List<WorkTask>(); History = new List<TaskHistory>(); Issues = new List<GitHubIssueInfo>(); }
    }
    public interface IAgentTool { string Name { get; } bool IsWrite { get; } Task<AgentToolResult> ExecuteAsync(AgentToolCall call, CancellationToken token); }
    public sealed class AgentToolRegistry
    {
        private readonly Dictionary<string, IAgentTool> tools = new Dictionary<string, IAgentTool>(StringComparer.OrdinalIgnoreCase);
        public AgentToolRegistry(IEnumerable<IAgentTool> values) { foreach (IAgentTool value in values) tools[value.Name] = value; }
        public void Register(IAgentTool value) { if(value!=null)tools[value.Name]=value; }
        public IAgentTool Find(string name) { IAgentTool value; return !string.IsNullOrWhiteSpace(name) && tools.TryGetValue(name, out value) ? value : null; }
        public IEnumerable<string> Names { get { return tools.Keys.OrderBy(x => x); } }
    }
    public sealed class AgentToolExecutor
    {
        private readonly AgentToolRegistry registry; private readonly ConversationContext context; private readonly IAgentLogger logger;
        public AgentToolExecutor(AgentToolRegistry registry, ConversationContext context, IAgentLogger logger) { this.registry = registry; this.context = context; this.logger = logger; }
        public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, CancellationToken token)
        {
            DateTime started = DateTime.UtcNow; IAgentTool tool = registry.Find(call == null ? null : call.Tool);
            if (tool == null) return new AgentToolResult { Success = false, ErrorType = "UnknownTool", SafeErrorMessage = "지원하지 않는 작업입니다." };
            logger.Info("Tool Decision", "tool=" + tool.Name + ",write=" + tool.IsWrite);
            try { AgentToolResult result = await tool.ExecuteAsync(call, token); result.Tool = tool.Name; context.RecordTool(tool.Name, result.Success ? "success" : (result.ErrorType ?? "failure"), result.TaskId); logger.Info("Executed Action", "tool=" + tool.Name + ",success=" + result.Success); if (tool.Name == "create_task" && result.Success && result.TaskId.HasValue) context.RecordCreated(result.TaskId.Value, result.Title); if (result.Tasks != null && result.Tasks.Count > 0) context.RecordRead(result.Tasks.Select(x => x.Id)); if (result.Issues != null && result.Issues.Count > 0) context.ReferenceGitHub(result.Issues[0].Number, false); int issueNumber; if (result.GitHubCreated && int.TryParse(result.GitHubIssueNumber, out issueNumber)) context.ReferenceGitHub(issueNumber, true); logger.Info("Tool Result", "tool=" + tool.Name + ",success=" + result.Success + ",taskId=" + (result.TaskId.HasValue ? result.TaskId.ToString() : "none") + ",elapsed=" + (long)(DateTime.UtcNow - started).TotalMilliseconds); return result; }
            catch (Exception ex) { logger.Error("Tool Failure " + tool.Name, ex); return new AgentToolResult { Tool = tool.Name, Success = false, ErrorType = ex.GetType().Name, SafeErrorMessage = "작업을 처리하지 못했습니다." }; }
        }
    }
    public sealed class WorkTaskAgentTool : IAgentTool
    {
        private readonly string name; private readonly TaskService tasks; private readonly IGitHubIssueCreationService creation; private readonly IGitHubSyncService sync; private readonly IClock clock; private readonly IGitHubService github; private readonly GitHubSettingsService githubSettings; private readonly IGitHubTaskMappingService mappings;
        public WorkTaskAgentTool(string name, TaskService tasks, IGitHubIssueCreationService creation, IGitHubSyncService sync, IClock clock) : this(name, tasks, creation, sync, clock, null, null, null) { }
        public WorkTaskAgentTool(string name, TaskService tasks, IGitHubIssueCreationService creation, IGitHubSyncService sync, IClock clock, IGitHubService github, GitHubSettingsService githubSettings, IGitHubTaskMappingService mappings) { this.name = name; this.tasks = tasks; this.creation = creation; this.sync = sync; this.clock = clock; this.github = github; this.githubSettings = githubSettings; this.mappings = mappings; }
        public string Name { get { return name; } } public bool IsWrite { get { return name == "create_task" || name == "start_task" || name == "complete_task" || name == "reopen_task" || name == "block_task" || name == "add_task_memo" || name == "update_task_description" || name == "update_task_due_date" || name == "update_task_priority" || name=="update_task_time" || name=="update_task_reminder" || name == "create_github_issue" || name == "retry_github_sync" || name == "delete_task" || name == "delete_tasks" || name == "import_github_issue"; } }
        public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, CancellationToken token)
        {
            if (name == "create_task") return await CreateAsync(call);
            if (name == "get_today_tasks" || name == "search_tasks" || name == "get_recent_tasks") return await ListAsync(call);
            if (name == "work_briefing") return await BriefingAsync(call);
            if (name == "get_recent_history") return await HistoryAsync();
            if (name == "refresh_github_issues") return await RefreshGitHubAsync(token);
            if (name == "import_github_issue") return await ImportGitHubAsync(call, token);
            if (name == "delete_task" || name == "delete_tasks") return await DeleteAsync(call);
            WorkTask task = await ResolveAsync(call); if (task == null) return Fail("TaskNotFound", "대상 업무를 찾지 못했습니다.");
            if (name == "get_task") return Ok(task, task.Title + " · " + task.Status);
            if (name == "get_task_github_status") return await GetGitHubStatusAsync(task, token);
            if (name == "start_task") await tasks.StartAsync(task);
            else if (name == "complete_task") await tasks.CompleteAsync(task);
            else if (name == "reopen_task") await tasks.MoveToTodoAsync(task);
            else if (name == "block_task") await tasks.BlockAsync(task, Arg(call, "reason") ?? "확인 필요");
            else if (name == "add_task_memo") { string memo = Arg(call, "memo"); if (string.IsNullOrWhiteSpace(memo)) return Fail("InvalidArguments", "기록할 내용을 알려주세요."); await tasks.AddMemoAsync(task, memo); }
            else if (name == "update_task_description") { string description = Arg(call, "description"); if (description == null) return Fail("InvalidArguments", "변경할 업무 설명을 알려주세요."); await tasks.UpdateDescriptionAsync(task, description); }
            else if (name == "update_task_due_date") { bool remove = string.Equals(Arg(call, "remove"), "true", StringComparison.OrdinalIgnoreCase); DateTimeOffset? due = null; if (!remove) { DateTime parsed; if (!DateTime.TryParse(Arg(call, "dueDate"), out parsed)) return Fail("InvalidDate", "INVALID_DATE: 변경할 날짜가 올바르지 않습니다."); due = parsed; } bool changed = await tasks.UpdateDueDateAsync(task, due); task = await tasks.GetByIdAsync(task.Id); return Ok(task, changed ? remove ? task.Title + "의 일정을 제거했습니다." : task.Title + " 일정을 " + Arg(call, "dateLabel") + "로 변경했습니다." : task.Title + "은 이미 " + (remove ? "일정이 없습니다." : Arg(call, "dateLabel") + " 일정입니다.")); }
            else if (name == "update_task_priority") { TaskPriority priority; if (!Enum.TryParse(Arg(call,"priority"),true,out priority)) return Fail("InvalidPriority","INVALID_PRIORITY"); bool changed=await tasks.UpdatePriorityAsync(task,priority);task=await tasks.GetByIdAsync(task.Id);string label=priority==TaskPriority.Critical?"긴급":priority==TaskPriority.High?"높음":priority==TaskPriority.Low?"낮음":"보통";return Ok(task,changed?task.Title+" 우선순위를 "+label+"으로 변경했습니다.":task.Title+"은 이미 우선순위가 "+label+"입니다."); }
            else if(name=="update_task_time"){bool remove=Arg(call,"remove")=="true";TimeSpan parsed=TimeSpan.Zero;if(!remove&&!TimeSpan.TryParse(Arg(call,"dueTime"),out parsed))return Fail("InvalidTime","INVALID_TIME");DateTime date;if(!remove&&DateTime.TryParse(Arg(call,"dueDate"),out date))await tasks.UpdateDueDateAsync(task,date);int offset;int? custom=int.TryParse(Arg(call,"reminderOffset"),out offset)?(int?)offset:null;bool changed=await tasks.UpdateTimeAsync(task,remove?(TimeSpan?)null:parsed,custom);task=await tasks.GetByIdAsync(task.Id);return Ok(task,changed?(remove?task.Title+"의 시간을 제거했습니다.":task.Title+" 시간을 "+task.DueTime.Value.ToString(@"hh\:mm")+"로 변경했습니다."):task.Title+"은 이미 같은 시간입니다.");}
            else if(name=="update_task_reminder"){bool enabled=Arg(call,"enabled")=="true";if(enabled&&!task.DueTime.HasValue)return Fail("TimeRequired","TIME_REQUIRED: 시간이 있는 업무에서만 알림을 켤 수 있습니다.");bool changed=await tasks.SetReminderEnabledAsync(task,enabled);task=await tasks.GetByIdAsync(task.Id);return Ok(task,changed?task.Title+" 알림을 "+(enabled?"켰습니다.":"껐습니다."):task.Title+" 알림은 이미 "+(enabled?"켜져 있습니다.":"꺼져 있습니다."));}
            else if (name == "create_github_issue") { if (creation == null) return Fail("NotConfigured", "GitHub 생성 서비스가 연결되지 않았습니다."); GitHubTaskCreationResult created = await creation.CreateForTaskAsync(task, token); return FromCreation(created); }
            else if (name == "retry_github_sync") { if (sync == null) return Fail("NotConfigured", "GitHub 동기화 서비스가 연결되지 않았습니다."); await sync.RetryTaskAsync(task); task = await tasks.GetByIdAsync(task.Id); }
            else return Fail("UnknownTool", "지원하지 않는 작업입니다.");
            task = await tasks.GetByIdAsync(task.Id); return Ok(task, ActionMessage(name, task));
        }
        private async Task<AgentToolResult> CreateAsync(AgentToolCall call)
        {
            string title = Arg(call, "title"); if (string.IsNullOrWhiteSpace(title)) return Fail("InvalidArguments", "업무 제목을 알려주세요."); DateTime? due = null; DateTime parsed; if (DateTime.TryParse(Arg(call, "dueDate"), out parsed)) due = parsed; TaskPriority priority; if(!Enum.TryParse(Arg(call,"priority")??"Normal",true,out priority))priority=TaskPriority.Normal;
            WorkTask task = await tasks.CreateTaskAsync(title, Arg(call, "description"), null, priority, due); TimeSpan dueTime;if(TimeSpan.TryParse(Arg(call,"dueTime"),out dueTime)){int offset;await tasks.UpdateTimeAsync(task,dueTime,int.TryParse(Arg(call,"reminderOffset"),out offset)?(int?)offset:null);} task = await tasks.GetByIdAsync(task.Id);
            AgentToolResult result = Ok(task, KoreanParticleHelper.Object(task.Title) + " 등록했습니다."); result.GitHubCreated = task.Source == TaskSource.GitHub; result.GitHubIssueNumber = task.ExternalId; result.ProjectAdded = !string.IsNullOrWhiteSpace(task.GitHubProjectItemId);
            if (result.GitHubCreated) result.Message += "\nGitHub Issue #" + task.ExternalId + "에도 등록했습니다."; else if (tasks.GitHubCreationService != null && tasks.GitHubCreationService.IsAutoCreateEnabled) { GitHubTaskCreationResult failure = tasks.LastGitHubCreationResult; result.Success = true; result.StatusCode = failure == null ? null : failure.StatusCode; result.ErrorType = failure == null ? "GitHubCreateFailed" : failure.ErrorType; result.SafeErrorMessage = failure == null ? "GitHub Issue 생성은 실패했습니다." : failure.SafeErrorMessage; result.Message += "\nGitHub Issue 생성은 실패했습니다. " + result.SafeErrorMessage; }
            return result;
        }
        private async Task<AgentToolResult> ListAsync(AgentToolCall call)
        {
            IEnumerable<WorkTask> values = await tasks.GetAllAsync(); string query = Arg(call, "query");
            if (name == "get_today_tasks") values = values.Where(x => !x.DueDate.HasValue || x.DueDate.Value.Date <= clock.Now.Date);
            else if (name == "get_recent_tasks") values = values.OrderByDescending(x => x.CreatedAt).Take(Math.Max(1, ParseInt(Arg(call, "count"), 5)));
            else { string status = Arg(call, "status"); WorkTaskStatus parsedStatus; TaskQuery specification = new TaskQuery { Keyword = query }; if (string.Equals(Arg(call, "statusMode"), "NotDone", StringComparison.OrdinalIgnoreCase)) specification.StatusFilter = TaskStatusFilter.NotDone; else if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkTaskStatus>(status, true, out parsedStatus)) { specification.Status = parsedStatus; specification.StatusFilter = TaskStatusFilter.Exact; } DateTime start, end; TaskDateField field; if (Enum.TryParse(Arg(call, "dateType"), true, out field) && DateTime.TryParse(Arg(call, "dateStart"), out start) && DateTime.TryParse(Arg(call, "dateEnd"), out end)) { specification.DateField = field; specification.StartDate = start; specification.EndDate = end; } values = await tasks.QueryAsync(specification); if (string.Equals(Arg(call, "transitionalToday"), "true", StringComparison.OrdinalIgnoreCase) && !values.Any()) { specification.DateField = TaskDateField.Created; specification.Status = null; values = (await tasks.QueryAsync(specification)).Where(x => specification.StatusFilter != TaskStatusFilter.NotDone || x.Status != WorkTaskStatus.Done); } }
            TaskPriority filterPriority; if(Enum.TryParse(Arg(call,"priority"),true,out filterPriority))values=values.Where(x=>x.Priority==filterPriority); DateTime completedDate; if (DateTime.TryParse(Arg(call, "completedDate"), out completedDate)) values = values.Where(x => x.CompletedAt.HasValue && x.CompletedAt.Value.Date == completedDate.Date); DateTime dueDate; if (DateTime.TryParse(Arg(call, "dueDate"), out dueDate)) values = values.Where(x => !x.DueDate.HasValue || x.DueDate.Value.Date <= dueDate.Date);
            int limit = ParseInt(Arg(call, "limit"), 0); if (limit > 0) values = values.OrderByDescending(x => x.CreatedAt).Take(limit); List<WorkTask> list = values.OrderByDescending(x => x.UpdatedAt).ToList(); return new AgentToolResult { Success = true, Tasks = list, IncludeCreatedAt = string.Equals(Arg(call, "includeCreatedAt"), "true", StringComparison.OrdinalIgnoreCase), IncludeDescription = string.Equals(Arg(call, "includeDescription"), "true", StringComparison.OrdinalIgnoreCase), QueryLabel = Arg(call, "queryLabel"), DateFallback = string.Equals(Arg(call, "transitionalToday"), "true", StringComparison.OrdinalIgnoreCase) && list.Count > 0 && list.All(x => !x.DueDate.HasValue) ? "DATE_FALLBACK_CREATED_TODAY" : null, Message = list.Count + "건을 찾았습니다." };
        }
        private async Task<AgentToolResult> BriefingAsync(AgentToolCall call)
        {
            bool urgent=string.Equals(Arg(call,"mode"),"urgent",StringComparison.OrdinalIgnoreCase);DateTime today=clock.Now.Date;IEnumerable<WorkTask> values=(await tasks.GetAllAsync()).Where(x=>x.Status!=WorkTaskStatus.Done);
            if(urgent)values=values.Where(x=>x.Priority==TaskPriority.Critical||x.Priority==TaskPriority.High).OrderBy(x=>x.Priority==TaskPriority.Critical?0:1).ThenBy(x=>x.DueDate??DateTimeOffset.MaxValue).ThenBy(x=>x.CreatedAt).ThenBy(x=>x.Id);
            else values=values.Where(x=>(x.DueDate.HasValue&&x.DueDate.Value.Date<=today)||(!x.DueDate.HasValue&&x.Priority==TaskPriority.Critical)).OrderBy(x=>BriefingGroup(x,today)).ThenBy(x=>x.DueDate??DateTimeOffset.MaxValue).ThenBy(x=>x.CreatedAt).ThenBy(x=>x.Id);
            List<WorkTask> list=values.ToList();StringBuilder text=new StringBuilder();if(list.Count==0)text.Append(urgent?"남아 있는 긴급 또는 높은 우선순위 업무가 없습니다.":"오늘 예정되거나 밀린 미완료 업무가 없습니다.");else{text.Append(urgent?"우선 확인할 급한 업무는 ":"오늘 우선 볼 업무는 ").Append(list.Count).Append("건입니다.");for(int i=0;i<list.Count;i++){WorkTask task=list[i];text.Append("\n").Append(i+1).Append(". ");if(task.Status==WorkTaskStatus.Blocked)text.Append("[막힘]");text.Append("[").Append(task.PriorityDisplayText).Append("]");if(task.DueDate.HasValue)text.Append(task.DueDate.Value.Date<today?"[기한 지남]":"[오늘]");else text.Append("[일정 없음]");text.Append(" ").Append(task.Title);}}
            return new AgentToolResult{Success=true,Tasks=list,Message=text.ToString(),QueryLabel=urgent?"급한 업무":"오늘 우선 업무"};
        }
        private static int BriefingGroup(WorkTask task,DateTime today){if(!task.DueDate.HasValue)return 8;bool overdue=task.DueDate.Value.Date<today;if(task.Priority==TaskPriority.Critical)return overdue?0:1;if(task.Priority==TaskPriority.High)return overdue?2:3;if(task.Priority==TaskPriority.Normal)return overdue?4:5;return overdue?6:7;}
        private async Task<AgentToolResult> HistoryAsync() { IReadOnlyList<TaskHistory> all = await tasks.GetHistoryAsync(); return new AgentToolResult { Success = true, History = all.OrderByDescending(x => x.CreatedAt).Take(30).ToList(), Message = "최근 활동을 조회했습니다." }; }
        private async Task<AgentToolResult> RefreshGitHubAsync(CancellationToken token)
        {
            if (github == null || githubSettings == null) return Fail("NotConfigured", "GitHub 조회 서비스가 연결되지 않았습니다."); GitHubIssuesResult result = await github.GetIssuesAsync(githubSettings.Load(), new GitHubIssueQuery { State = GitHubIssueState.All, Page = 1, PerPage = 50, Sort = GitHubIssueSort.RecentlyUpdated }, token); if (!result.Success) return new AgentToolResult { Success = false, StatusCode = result.StatusCode.HasValue ? (int?)result.StatusCode.Value : null, ErrorType = result.Status.ToString(), SafeErrorMessage = result.Message, Message = result.Message }; if (mappings != null) foreach (GitHubIssueInfo issue in result.Issues) { WorkTask tracked = await mappings.GetTrackedTaskAsync(issue.Number); issue.IsTracked = tracked != null; issue.TrackedTaskId = tracked == null ? (Guid?)null : tracked.Id; } return new AgentToolResult { Success = true, Issues = result.Issues.OrderByDescending(x => x.CreatedAt).ToList(), Message = "GitHub Issue를 새로고침했습니다." };
        }
        private async Task<AgentToolResult> GetGitHubStatusAsync(WorkTask task, CancellationToken token)
        {
            if (task.Source != TaskSource.GitHub || string.IsNullOrWhiteSpace(task.ExternalId)) return Ok(task, KoreanParticleHelper.Topic(task.Title) + " GitHub에 등록되지 않았습니다."); int number; if (!int.TryParse(task.ExternalId, out number)) return Fail("InvalidMapping", "GitHub Issue 번호가 올바르지 않습니다."); if (github == null || githubSettings == null) return Ok(task, GitHubStatus(task)); GitHubIssueResult actual = await github.GetIssueAsync(githubSettings.Load(), number, token); if (!actual.Success || actual.Issue == null) return new AgentToolResult { Success = false, TaskId = task.Id, StatusCode = actual.StatusCode.HasValue ? (int?)actual.StatusCode.Value : null, ErrorType = actual.Status.ToString(), SafeErrorMessage = actual.Message, Message = actual.Message }; return Ok(task, KoreanParticleHelper.Topic(task.Title) + " GitHub Issue #" + number + "로 등록되어 있으며 실제 상태는 " + actual.Issue.State + "입니다. Issue Sync: " + task.GitHubSyncStatus + ", Project Sync: " + task.GitHubProjectSyncStatus + ".");
        }
        private async Task<AgentToolResult> ImportGitHubAsync(AgentToolCall call, CancellationToken token)
        {
            int number = ParseInt(Arg(call, "issueNumber"), 0); if (number <= 0) return Fail("InvalidArguments", "가져올 GitHub Issue 번호가 필요합니다."); if (github == null || githubSettings == null || mappings == null) return Fail("NotConfigured", "GitHub Import 서비스가 연결되지 않았습니다."); GitHubIssueResult issue = await github.GetIssueAsync(githubSettings.Load(), number, token); if (!issue.Success || issue.Issue == null) return new AgentToolResult { Success = false, StatusCode = issue.StatusCode.HasValue ? (int?)issue.StatusCode.Value : null, ErrorType = issue.Status.ToString(), SafeErrorMessage = issue.Message, Message = issue.Message }; WorkTask task = await mappings.TrackIssueAsync(issue.Issue); return Ok(task, "GitHub Issue #" + number + "을 SlaveSplit 업무로 가져왔습니다.");
        }
        private async Task<AgentToolResult> DeleteAsync(AgentToolCall call)
        {
            List<Guid> ids = new List<Guid>(); Guid one; if (Guid.TryParse(Arg(call, "taskId"), out one)) ids.Add(one); string raw = Arg(call, "taskIds"); if (!string.IsNullOrWhiteSpace(raw)) foreach (string value in raw.Split(',')) { Guid id; if (Guid.TryParse(value.Trim(), out id) && !ids.Contains(id)) ids.Add(id); } if (ids.Count == 0) return Fail("InvalidArguments", "삭제할 업무가 선택되지 않았습니다."); int deleted = 0;
            foreach (Guid id in ids) { WorkTask task = await tasks.GetByIdAsync(id); if (task == null) continue; if (task.Source == TaskSource.GitHub && !string.IsNullOrWhiteSpace(task.ExternalId)) { if (task.Status != WorkTaskStatus.Done) await tasks.CompleteAsync(task, "Local task deletion: GitHub Issue closed"); task = await tasks.GetByIdAsync(id); if (task.GitHubSyncStatus == GitHubSyncStatus.Pending || task.GitHubProjectSyncStatus == GitHubSyncStatus.Pending) return Fail("GitHubSyncPending", "GitHub 또는 Project 정리가 대기 중이어서 Local 업무를 삭제하지 않았습니다. 동기화 후 다시 시도해 주세요."); } await tasks.DeleteAsync(id); deleted++; }
            return new AgentToolResult { Success = true, Message = deleted + "건의 Local 업무를 삭제했습니다. 연결된 GitHub Issue는 Close하고 Project 상태는 Done으로 정리했습니다." };
        }
        private async Task<WorkTask> ResolveAsync(AgentToolCall call) { Guid id; if (Guid.TryParse(Arg(call, "taskId"), out id)) return await tasks.GetByIdAsync(id); string query = Arg(call, "query") ?? Arg(call, "title"); if (string.IsNullOrWhiteSpace(query)) return null; return (await tasks.GetAllAsync()).Where(x => (x.Title ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(x => x.UpdatedAt).FirstOrDefault(); }
        private static string Arg(AgentToolCall call, string key) { string value; return call != null && call.Arguments != null && call.Arguments.TryGetValue(key, out value) ? value : null; }
        private static int ParseInt(string value, int fallback) { int parsed; return int.TryParse(value, out parsed) ? parsed : fallback; }
        private static AgentToolResult Ok(WorkTask task, string message) { return new AgentToolResult { Success = true, TaskId = task.Id, Title = task.Title, Message = message, GitHubIssueNumber = task.ExternalId, ProjectAdded = !string.IsNullOrWhiteSpace(task.GitHubProjectItemId), GitHubSyncStatus = task.GitHubSyncStatus, GitHubProjectSyncStatus = task.GitHubProjectSyncStatus, GitHubSyncError = task.LastGitHubSyncError }; }
        private static AgentToolResult Fail(string type, string message) { return new AgentToolResult { Success = false, ErrorType = type, SafeErrorMessage = message, Message = message }; }
        private static AgentToolResult FromCreation(GitHubTaskCreationResult value) { return new AgentToolResult { Success = value.Success, TaskId = value.Task == null ? (Guid?)null : value.Task.Id, Title = value.Task == null ? null : value.Task.Title, Message = value.Message, SafeErrorMessage = value.Success ? null : value.SafeErrorMessage ?? value.Message, ErrorType = value.ErrorType, StatusCode = value.StatusCode, GitHubCreated = value.Success, GitHubIssueNumber = value.Issue == null ? null : value.Issue.Number.ToString(), ProjectAdded = value.Task != null && !string.IsNullOrWhiteSpace(value.Task.GitHubProjectItemId) }; }
        private static string GitHubStatus(WorkTask task) { if (task.Source != TaskSource.GitHub || string.IsNullOrWhiteSpace(task.ExternalId)) return KoreanParticleHelper.Topic(task.Title) + " GitHub에 등록되지 않았습니다."; return KoreanParticleHelper.Topic(task.Title) + " GitHub Issue #" + task.ExternalId + "로 등록되어 있습니다. Issue Sync: " + task.GitHubSyncStatus + ", Project Sync: " + task.GitHubProjectSyncStatus + "."; }
        private static string ActionMessage(string action, WorkTask task) { string title = KoreanParticleHelper.Object(task.Title); if (action == "start_task") return title + " 진행 중으로 변경했습니다."; if (action == "complete_task") return title + " 완료했습니다."; if (action == "reopen_task") return title + " 다시 열었습니다."; if (action == "block_task") return title + " 대기 상태로 변경했습니다."; if (action == "add_task_memo") return task.Title + "에 진행사항을 기록했습니다."; if (action == "update_task_description") return task.Title + "의 설명을 수정했습니다."; return task.Title + "의 GitHub 동기화를 다시 시도했습니다."; }
    }
}
