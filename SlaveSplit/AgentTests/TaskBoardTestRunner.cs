using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Models;
using SlaveSplit.Services;
using SlaveSplit.ViewModels;

namespace SlaveSplit.AgentTests
{
    public sealed class TaskBoardTestRunner
    {
        public async Task<IList<AgentTestResult>> RunAsync()
        {
            var results=new List<AgentTestResult>();
            await Test(results,"TEST-032-01","Status Group",async c=>{await c.Add("A",WorkTaskStatus.Todo);await c.Add("B",WorkTaskStatus.InProgress);await c.Add("C",WorkTaskStatus.Blocked);await c.Add("D",WorkTaskStatus.Done);await c.Board.LoadAsync();return Names(c.Board.Todo)=="A"&&Names(c.Board.InProgress)=="B"&&Names(c.Board.Blocked)=="C"&&Names(c.Board.Done)=="D";});
            await Test(results,"TEST-032-02","Count",async c=>{for(int i=0;i<3;i++)await c.Add("T"+i,WorkTaskStatus.Todo);for(int i=0;i<2;i++)await c.Add("P"+i,WorkTaskStatus.InProgress);await c.Add("B",WorkTaskStatus.Blocked);for(int i=0;i<4;i++)await c.Add("D"+i,WorkTaskStatus.Done);await c.Board.LoadAsync();return c.Board.Todo.Count==3&&c.Board.InProgress.Count==2&&c.Board.Blocked.Count==1&&c.Board.Done.Count==4;});
            await Test(results,"TEST-032-03","Workspace Isolation",async c=>{await c.Add("A1",WorkTaskStatus.Todo,"workspace-a");await c.Add("B1",WorkTaskStatus.Todo,"workspace-b");await c.Add("B2",WorkTaskStatus.Done,"workspace-b");c.Workspace="workspace-b";await c.Board.LoadAsync();return Names(c.Board.Todo)=="B1"&&Names(c.Board.Done)=="B2";});
            await Test(results,"TEST-032-04","TODO to IN_PROGRESS",async c=>{var x=await c.Add("A",WorkTaskStatus.Todo);return await c.Board.MoveAsync(x,WorkTaskStatus.InProgress)&&x.Status==WorkTaskStatus.InProgress;});
            await Test(results,"TEST-032-05","IN_PROGRESS to DONE",async c=>{var x=await c.Add("A",WorkTaskStatus.InProgress);return await c.Board.MoveAsync(x,WorkTaskStatus.Done)&&x.CompletedAt.HasValue&&x.CompletedAt.Value.LocalDateTime==c.Clock.Now;});
            await Test(results,"TEST-032-06","DONE to IN_PROGRESS",async c=>{var x=await c.Add("A",WorkTaskStatus.Done);x.CompletedAt=c.Clock.Now.AddHours(-1);return await c.Board.MoveAsync(x,WorkTaskStatus.InProgress)&&!x.CompletedAt.HasValue;});
            await Test(results,"TEST-032-07","Same Column",async c=>{var x=await c.Add("A",WorkTaskStatus.Todo);return !await c.Board.MoveAsync(x,WorkTaskStatus.Todo)&&x.Status==WorkTaskStatus.Todo;});
            await Test(results,"TEST-032-08","Invalid Workspace Drop",async c=>{var x=await c.Add("A",WorkTaskStatus.Todo,"workspace-a");c.Workspace="workspace-b";return !await c.Board.MoveAsync(x,WorkTaskStatus.Done)&&x.Status==WorkTaskStatus.Todo;});
            await Test(results,"TEST-032-09","Overdue",async c=>{var a=await c.Add("A",WorkTaskStatus.Todo);a.DueDate=DateTime.Today.AddDays(-1);var b=await c.Add("B",WorkTaskStatus.Done);b.DueDate=DateTime.Today.AddDays(-1);return a.IsOverdue&&!b.IsOverdue;});
            await Test(results,"TEST-032-10","DueTime Display",async c=>{var a=await c.Add("A",WorkTaskStatus.Todo);a.DueDate=DateTime.Today;a.DueTime=new TimeSpan(18,0,0);var b=await c.Add("B",WorkTaskStatus.Todo);b.DueDate=DateTime.Today;return a.DueDateTimeLabel.Contains("18:00")&&!b.DueDateTimeLabel.Contains(":");});
            await Test(results,"TEST-032-11","Priority Display",async c=>{var a=await c.Add("A",WorkTaskStatus.Todo);a.Priority=TaskPriority.Critical;var b=await c.Add("B",WorkTaskStatus.Todo);b.Priority=TaskPriority.High;var d=await c.Add("D",WorkTaskStatus.Todo);d.Priority=TaskPriority.Low;return a.PriorityText=="긴급"&&b.PriorityText=="높음"&&d.PriorityText=="낮음";});
            await Test(results,"TEST-032-12","External Refresh",async c=>{var x=await c.Add("A",WorkTaskStatus.Todo);await c.Board.LoadAsync();await c.Service.CompleteAsync(x);await c.Board.LoadAsync();return c.Board.Todo.Count==0&&Names(c.Board.Done)=="A";});
            await Test(results,"TEST-032-13","Board to Repository",async c=>{var x=await c.Add("A",WorkTaskStatus.Todo);await c.Board.MoveAsync(x,WorkTaskStatus.InProgress);return (await c.Service.GetByIdAsync(x.Id)).Status==WorkTaskStatus.InProgress;});
            await Test(results,"TEST-032-14","Failure Rollback",async c=>{var x=await c.Add("A",WorkTaskStatus.Todo);c.Repository.FailUpdates=true;bool moved=await c.Board.MoveAsync(x,WorkTaskStatus.Done);return !moved&&x.Status==WorkTaskStatus.Todo&&c.Board.Todo.Tasks.Any(t=>t.Id==x.Id);});
            await Regression(results,"TEST-032-15","Chat Status Regression","TEST-029-11");
            await Regression(results,"TEST-032-16","DueDate Regression","TEST-031-17");
            await Regression(results,"TEST-032-17","Priority Regression","TEST-031-18");
            await Regression(results,"TEST-032-18","Briefing Regression","TEST-031-19");
            await Regression(results,"TEST-032-19","Reminder Regression","TEST-031-02");
            return results;
        }
        private static async Task Regression(ICollection<AgentTestResult> results,string id,string name,string sourceId){var runner=new AgentScenarioTestRunner();var source=AgentTestScenarioRegistry.All().Where(x=>x.Id==sourceId);var actual=await runner.RunAsync(source);bool pass=actual.Count>0&&actual.All(x=>x.Passed);results.Add(new AgentTestResult{ScenarioId=id,Group="Task-032",Scenario=name,Step=1,Input=name,Expected="PASS",Actual=pass?"PASS":"FAIL",Status=pass?"PASS":"FAIL",FailureReason=pass?null:string.Join("; ",actual.Where(x=>!x.Passed).Select(x=>x.FailureReason)),Mode=AgentTestMode.Fast,Source="REGRESSION"});}
        private static async Task Test(ICollection<AgentTestResult> results,string id,string name,Func<Context,Task<bool>> action){bool pass=false;string failure=null;try{pass=await action(new Context());if(!pass)failure="Board assertion failed.";}catch(Exception ex){failure=ex.Message;}results.Add(new AgentTestResult{ScenarioId=id,Group="Task-032",Scenario=name,Step=1,Input=name,Expected="PASS",Actual=pass?"PASS":"FAIL",Status=pass?"PASS":"FAIL",FailureReason=failure,Mode=AgentTestMode.Fast,Source="BOARD",ActualMutationCount=0});}
        private static string Names(TaskBoardColumnViewModel c){return string.Join("|",c.Tasks.Select(x=>x.Title));}
        private sealed class Context
        {
            public Context(){Repository=new InMemoryTaskRepository();Clock=new AgentTestClock{Now=new DateTime(2026,9,2,14,0,0)};Service=new TaskService(Repository,Clock);Board=new TaskBoardViewModel(Service,new TaskNavigationService(),()=>Workspace);}
            public InMemoryTaskRepository Repository;public AgentTestClock Clock;public TaskService Service;public TaskBoardViewModel Board;public string Workspace;
            public async Task<WorkTask> Add(string title,WorkTaskStatus status,string workspace=null){var x=await Service.CreateTaskAsync(title,allowGitHubAutoCreate:false);x.Status=status;x.WorkspaceId=workspace;if(status==WorkTaskStatus.Done)x.CompletedAt=Clock.Now;await Repository.UpdateAsync(x);return x;}
        }
    }
}
