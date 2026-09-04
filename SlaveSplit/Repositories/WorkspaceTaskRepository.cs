using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SlaveSplit.Models;
using SlaveSplit.Services;

namespace SlaveSplit.Repositories
{
    public sealed class WorkspaceTaskRepository : ITaskRepository
    {
        private readonly ITaskRepository inner; private readonly WorkspaceService workspaces;
        public WorkspaceTaskRepository(ITaskRepository inner,WorkspaceService workspaces){this.inner=inner;this.workspaces=workspaces;}
        private bool InScope(WorkTask x){return string.Equals(string.IsNullOrWhiteSpace(x.WorkspaceId)?workspaces.Workspaces[0].Id:x.WorkspaceId,workspaces.CurrentScopeId,StringComparison.OrdinalIgnoreCase);}
        public async Task<IReadOnlyList<WorkTask>> GetAllAsync(){return (await inner.GetAllAsync()).Where(InScope).ToList();}
        public async Task<WorkTask> GetByIdAsync(Guid id){WorkTask task=await inner.GetByIdAsync(id);return task!=null&&InScope(task)?task:null;}
        public Task AddAsync(WorkTask task){task.WorkspaceId=workspaces.CurrentScopeId;return inner.AddAsync(task);}
        public async Task UpdateAsync(WorkTask task){WorkTask existing=await GetByIdAsync(task.Id);if(existing==null)throw new KeyNotFoundException("Task is outside the active workspace.");task.WorkspaceId=workspaces.CurrentScopeId;await inner.UpdateAsync(task);}
        public async Task DeleteAsync(Guid id){if(await GetByIdAsync(id)==null)throw new KeyNotFoundException("Task is outside the active workspace.");await inner.DeleteAsync(id);}
        public async Task<IReadOnlyList<WorkTask>> GetByStatusAsync(WorkTaskStatus status){return (await GetAllAsync()).Where(x=>x.Status==status).ToList();}
        public async Task<IReadOnlyList<WorkTask>> GetCompletedByDateAsync(DateTime date){return (await GetAllAsync()).Where(x=>x.Status==WorkTaskStatus.Done&&x.CompletedAt.HasValue&&x.CompletedAt.Value.Date==date.Date).ToList();}
        public async Task<IReadOnlyList<WorkTask>> GetCreatedByDateAsync(DateTime date){return (await GetAllAsync()).Where(x=>x.CreatedAt.Date==date.Date).ToList();}
        public async Task<IReadOnlyList<WorkTask>> GetByDueDateAsync(DateTime date){return (await GetAllAsync()).Where(x=>x.DueDate.HasValue&&x.DueDate.Value.Date==date.Date).ToList();}
        public async Task<IReadOnlyList<WorkTask>> QueryAsync(TaskQuery query){IEnumerable<WorkTask> result=await GetAllAsync();if(query==null)return result.ToList();if(query.StatusFilter==TaskStatusFilter.NotDone)result=result.Where(x=>x.Status!=WorkTaskStatus.Done);else if(query.Status.HasValue)result=result.Where(x=>x.Status==query.Status);if(query.Priority.HasValue)result=result.Where(x=>x.Priority==query.Priority);if(!string.IsNullOrWhiteSpace(query.Category))result=result.Where(x=>string.Equals(x.Category,query.Category,StringComparison.OrdinalIgnoreCase));if(!string.IsNullOrWhiteSpace(query.Keyword))result=result.Where(x=>(x.Title??"").IndexOf(query.Keyword,StringComparison.OrdinalIgnoreCase)>=0||(x.Description??"").IndexOf(query.Keyword,StringComparison.OrdinalIgnoreCase)>=0);if(query.StartDate.HasValue||query.EndDate.HasValue)result=result.Where(x=>{bool completed=query.DateField==TaskDateField.Completed||query.UseCompletedDate;DateTime? d=query.DateField==TaskDateField.Due?(x.DueDate.HasValue?(DateTime?)x.DueDate.Value.LocalDateTime:null):completed?(x.CompletedAt.HasValue?(DateTime?)x.CompletedAt.Value.LocalDateTime:null):query.DateField==TaskDateField.Created||query.UseCreatedDate?(DateTime?)x.CreatedAt.LocalDateTime:(DateTime?)x.UpdatedAt;return(!completed||x.Status==WorkTaskStatus.Done)&&d.HasValue&&(!query.StartDate.HasValue||d>=query.StartDate)&&(!query.EndDate.HasValue||d<query.EndDate);});return result.ToList();}
        public Task AddHistoryAsync(TaskHistory history){return inner.AddHistoryAsync(history);}
        public async Task<IReadOnlyList<TaskHistory>> GetHistoryAsync(Guid? taskId=null){if(taskId.HasValue&&await GetByIdAsync(taskId.Value)==null)return new List<TaskHistory>();IReadOnlyList<TaskHistory> all=await inner.GetHistoryAsync(taskId);if(taskId.HasValue)return all;HashSet<Guid> ids=new HashSet<Guid>((await GetAllAsync()).Select(x=>x.Id));return all.Where(x=>ids.Contains(x.TaskId)).ToList();}
    }
}
