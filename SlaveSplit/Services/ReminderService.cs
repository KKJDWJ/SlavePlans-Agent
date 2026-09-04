using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;

namespace SlaveSplit.Services
{
    public interface INotificationService { void ShowReminder(WorkTask task,string workspaceName,int minutesUntilDue); }
    public sealed class FakeNotificationService : INotificationService { public IList<string> Notifications { get; private set; }=new List<string>(); public void ShowReminder(WorkTask task,string workspaceName,int minutes){Notifications.Add(workspaceName+"|"+task.Title+"|"+minutes);} }
    public sealed class ReminderService : IDisposable
    {
        private readonly TaskService tasks;private readonly WorkspaceService workspaces;private readonly IClock clock;private readonly INotificationService notifications;private readonly DateTimeOffset startedAt;private readonly DispatcherTimer timer;
        public ReminderService(TaskService tasks,WorkspaceService workspaces,IClock clock,INotificationService notifications,bool startTimer=true){this.tasks=tasks;this.workspaces=workspaces;this.clock=clock;this.notifications=notifications;startedAt=new DateTimeOffset(clock.Now);if(startTimer){timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(30)};timer.Tick+=async(s,e)=>await TickAsync();timer.Start();}}
        public async Task TickAsync(){IEnumerable<GitHubWorkspace> scopes=workspaces==null?new GitHubWorkspace[]{null}:workspaces.Workspaces;foreach(GitHubWorkspace workspace in scopes){using(workspaces==null?null:workspaces.Use(workspace.Id)){foreach(WorkTask task in await tasks.GetAllAsync()){DateTimeOffset? at=task.ReminderAt;if(task.Status==WorkTaskStatus.Done||!at.HasValue||at.Value<startedAt||at.Value>new DateTimeOffset(clock.Now)||task.LastReminderNotifiedAt==at)continue;int minutes=task.DueDateTime.HasValue?Math.Max(0,(int)Math.Round((task.DueDateTime.Value-new DateTimeOffset(clock.Now)).TotalMinutes)):0;notifications.ShowReminder(task,workspace==null?null:workspace.DisplayName,minutes);task.LastReminderNotifiedAt=at;await tasks.UpdateAsync(task);}}}}
        public void Dispose(){if(timer!=null)timer.Stop();}
    }
}
