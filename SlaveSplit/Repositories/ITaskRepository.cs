using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SlaveSplit.Models;
namespace SlaveSplit.Repositories
{
    public interface ITaskRepository
    {
        Task<IReadOnlyList<WorkTask>> GetAllAsync();
        Task<WorkTask> GetByIdAsync(Guid id);
        Task AddAsync(WorkTask task);
        Task UpdateAsync(WorkTask task);
        Task DeleteAsync(Guid id);
        Task<IReadOnlyList<WorkTask>> GetByStatusAsync(WorkTaskStatus status);
        Task<IReadOnlyList<WorkTask>> GetCompletedByDateAsync(DateTime date);
        Task<IReadOnlyList<WorkTask>> GetCreatedByDateAsync(DateTime date);
        Task<IReadOnlyList<WorkTask>> GetByDueDateAsync(DateTime date);
        Task<IReadOnlyList<WorkTask>> QueryAsync(TaskQuery query);
        Task AddHistoryAsync(TaskHistory history);
        Task<IReadOnlyList<TaskHistory>> GetHistoryAsync(Guid? taskId = null);
    }
}
