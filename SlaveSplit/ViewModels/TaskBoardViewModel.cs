using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SlaveSplit.Commands;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;

namespace SlaveSplit.ViewModels
{
    public sealed class TaskBoardColumnViewModel
    {
        public TaskBoardColumnViewModel(WorkTaskStatus status, string title) { Status = status; Title = title; Tasks = new ObservableCollection<WorkTask>(); }
        public WorkTaskStatus Status { get; private set; }
        public string Title { get; private set; }
        public ObservableCollection<WorkTask> Tasks { get; private set; }
        public int Count { get { return Tasks.Count; } }
    }

    public sealed class TaskBoardViewModel : ViewModelBase
    {
        private readonly TaskService service; private readonly TaskNavigationService navigation; private readonly Func<string> activeWorkspaceId; private bool isLoading; private string errorMessage;
        public TaskBoardViewModel(TaskService service, TaskNavigationService navigation, Func<string> activeWorkspaceId = null)
        {
            this.service = service; this.navigation = navigation; this.activeWorkspaceId = activeWorkspaceId;
            Todo = new TaskBoardColumnViewModel(WorkTaskStatus.Todo, "할 일"); InProgress = new TaskBoardColumnViewModel(WorkTaskStatus.InProgress, "진행 중"); Blocked = new TaskBoardColumnViewModel(WorkTaskStatus.Blocked, "보류"); Done = new TaskBoardColumnViewModel(WorkTaskStatus.Done, "완료"); Columns = new[] { Todo, InProgress, Blocked, Done };
            OpenTaskCommand = new RelayCommand(p => { WorkTask task = p as WorkTask; if (task != null) navigation.Open(task.Id); }); RefreshCommand = new RelayCommand(async p => await LoadAsync(), p => !IsLoading);
            service.TasksChanged += async (s, e) => await LoadAsync(); Initialize();
        }
        public TaskBoardColumnViewModel Todo { get; private set; } public TaskBoardColumnViewModel InProgress { get; private set; } public TaskBoardColumnViewModel Blocked { get; private set; } public TaskBoardColumnViewModel Done { get; private set; }
        public TaskBoardColumnViewModel[] Columns { get; private set; } public ICommand OpenTaskCommand { get; private set; } public ICommand RefreshCommand { get; private set; }
        public bool IsLoading { get { return isLoading; } private set { SetProperty(ref isLoading, value); } } public string ErrorMessage { get { return errorMessage; } private set { SetProperty(ref errorMessage, value); } }
        public async Task LoadAsync()
        {
            IsLoading = true;
            try { var all = await service.GetAllAsync(); string workspace=activeWorkspaceId==null?null:activeWorkspaceId(); if(!string.IsNullOrWhiteSpace(workspace))all=all.Where(x=>string.IsNullOrWhiteSpace(x.WorkspaceId)||string.Equals(x.WorkspaceId,workspace,StringComparison.OrdinalIgnoreCase)).ToList(); foreach (var column in Columns) column.Tasks.Clear(); foreach (WorkTask task in all.OrderByDescending(x => x.Priority).ThenBy(x => x.DueDate ?? DateTimeOffset.MaxValue).ThenBy(x => x.Title)) Column(task.Status).Tasks.Add(task); NotifyCounts(); ErrorMessage = null; }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally { IsLoading = false; }
        }
        public async Task<bool> MoveAsync(WorkTask task, WorkTaskStatus target)
        {
            if (task == null || task.Status == target) return false;
            string workspace = activeWorkspaceId == null ? null : activeWorkspaceId(); if (!string.IsNullOrWhiteSpace(workspace) && !string.IsNullOrWhiteSpace(task.WorkspaceId) && !string.Equals(task.WorkspaceId, workspace, StringComparison.OrdinalIgnoreCase)) return false;
            WorkTaskStatus oldStatus = task.Status; DateTimeOffset? oldCompleted = task.CompletedAt; DateTime? oldStarted = task.StartedAt; string oldBlocked = task.BlockedReason; DateTime oldUpdated = task.UpdatedAt;
            try { if (target == WorkTaskStatus.Todo) await service.MoveToTodoAsync(task, "보드에서 Todo로 이동"); else if (target == WorkTaskStatus.InProgress) await service.StartAsync(task, "보드에서 진행중으로 이동"); else if (target == WorkTaskStatus.Blocked) await service.BlockAsync(task, "보드에서 Blocked로 이동"); else await service.CompleteAsync(task, "보드에서 완료"); await LoadAsync(); return true; }
            catch (Exception ex) { task.Status = oldStatus; task.CompletedAt = oldCompleted; task.StartedAt = oldStarted; task.BlockedReason = oldBlocked; task.UpdatedAt = oldUpdated; await LoadAsync(); ErrorMessage = "상태 변경 실패: " + ex.Message; return false; }
        }
        private TaskBoardColumnViewModel Column(WorkTaskStatus status) { return status == WorkTaskStatus.InProgress ? InProgress : status == WorkTaskStatus.Blocked ? Blocked : status == WorkTaskStatus.Done ? Done : Todo; }
        private void NotifyCounts() { foreach (var column in Columns) OnPropertyChanged(column.Status + "Count"); OnPropertyChanged("Columns"); }
        private async void Initialize() { await LoadAsync(); }
    }
}
