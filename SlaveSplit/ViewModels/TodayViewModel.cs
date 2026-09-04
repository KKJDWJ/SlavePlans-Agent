using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SlaveSplit.Commands;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;
namespace SlaveSplit.ViewModels
{
    public sealed class TodayViewModel : ViewModelBase
    {
        private readonly TaskService service; private readonly TaskNavigationService navigation; private readonly DashboardNavigationService dashboardNavigation; private readonly DailySummaryService summaries; private readonly IClock clock; private DateTime displayedDate;
        private string newTitle, newDescription, newCategory = "기타", errorMessage; private TaskPriority newPriority = TaskPriority.Normal; private DateTime? newDueDate; private bool isAddPanelOpen;
        public TodayViewModel(TaskService service, TaskNavigationService navigation, DashboardNavigationService dashboardNavigation = null, DailySummaryService summaries = null, IClock clock = null)
        {
            this.service = service; this.navigation = navigation; this.dashboardNavigation = dashboardNavigation ?? new DashboardNavigationService(); this.clock = clock ?? new SystemClock(); this.summaries = summaries ?? new DailySummaryService(service, this.clock); displayedDate = this.clock.Now.Date;
            Tasks = new ObservableCollection<WorkTask>(); YesterdayCompleted = new ObservableCollection<WorkTask>(); Priorities = Enum.GetValues(typeof(TaskPriority)).Cast<TaskPriority>().ToArray();
            ToggleAddCommand = new RelayCommand(p => IsAddPanelOpen = !IsAddPanelOpen); QuickAddCommand = new RelayCommand(async p => await AddAsync(), p => !string.IsNullOrWhiteSpace(NewTitle)); AddTaskCommand = QuickAddCommand;
            StartCommand = new RelayCommand(async p => await ChangeAsync(p as WorkTask, t => service.StartAsync(t)));
            CompleteCommand = new RelayCommand(async p => await ChangeAsync(p as WorkTask, t => service.CompleteAsync(t)));
            BlockCommand = new RelayCommand(async p => await ChangeAsync(p as WorkTask, t => service.BlockAsync(t, string.IsNullOrWhiteSpace(t.BlockedReason) ? "사유 확인 필요" : t.BlockedReason)));
            ResumeCommand = new RelayCommand(async p => await ChangeAsync(p as WorkTask, t => service.UnblockAsync(t, true)));
            TodoCommand = new RelayCommand(async p => await ChangeAsync(p as WorkTask, t => service.MoveToTodoAsync(t)));
            OpenTaskCommand = new RelayCommand(p => { WorkTask task = p as WorkTask; if (task != null) navigation.Open(task.Id); }); DailySummaryCommand = new RelayCommand(p => this.dashboardNavigation.OpenDailySummary(displayedDate)); YesterdaySummaryCommand = new RelayCommand(p => this.dashboardNavigation.OpenDailySummary(displayedDate.AddDays(-1))); RefreshCommand = new RelayCommand(async p => await LoadAsync());
            service.TasksChanged += async (s, e) => await LoadAsync(); Initialize();
        }
        public ObservableCollection<WorkTask> Tasks { get; private set; } public ObservableCollection<WorkTask> YesterdayCompleted { get; private set; } public TaskPriority[] Priorities { get; private set; }
        public string Greeting { get { int hour = clock.Now.Hour; return hour < 12 ? "Good morning." : hour < 18 ? "Good afternoon." : "Good evening."; } }
        public string DateTitle { get { return displayedDate.ToString("yyyy.MM.dd dddd"); } }
        public System.Collections.Generic.IEnumerable<WorkTask> InProgressTasks { get { return Tasks.Where(x => x.Status == WorkTaskStatus.InProgress); } }
        public System.Collections.Generic.IEnumerable<WorkTask> TodoTasks { get { return Tasks.Where(x => x.Status == WorkTaskStatus.Todo && x.DueDate.HasValue && x.DueDate.Value.Date <= displayedDate).OrderByDescending(x => x.IsOverdue).ThenByDescending(x => x.Priority).ThenBy(x => x.CreatedAt); } }
        public System.Collections.Generic.IEnumerable<WorkTask> UpcomingTasks { get { return Tasks.Where(x => x.Status == WorkTaskStatus.Todo && x.DueDate.HasValue && x.DueDate.Value.Date > displayedDate).OrderBy(x => x.DueDate).Take(5); } }
        public System.Collections.Generic.IEnumerable<WorkTask> BacklogTasks { get { return Tasks.Where(x => x.Status == WorkTaskStatus.Todo && !x.DueDate.HasValue).OrderByDescending(x => x.Priority).ThenBy(x => x.CreatedAt).Take(5); } }
        public System.Collections.Generic.IEnumerable<WorkTask> CarryOverTasks { get { return Tasks.Where(x => x.Status != WorkTaskStatus.Done && x.Status != WorkTaskStatus.Blocked && x.DueDate.HasValue && x.DueDate.Value.Date < displayedDate).OrderBy(x => x.DueDate); } }
        public System.Collections.Generic.IEnumerable<WorkTask> BlockedTasks { get { return Tasks.Where(x => x.Status == WorkTaskStatus.Blocked); } }
        public System.Collections.Generic.IEnumerable<WorkTask> DoneTodayTasks { get { return Tasks.Where(x => x.Status == WorkTaskStatus.Done && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == displayedDate).OrderByDescending(x => x.CompletedAt); } }
        public int TodoCount { get { return TodoTasks.Count(); } } public int ProgressCount { get { return InProgressTasks.Count(); } } public int BlockedCount { get { return BlockedTasks.Count(); } } public int DoneCount { get { return DoneTodayTasks.Count(); } } public int YesterdayCount { get { return YesterdayCompleted.Count; } }
        public bool HasNoActiveWork { get { return TodoCount == 0 && ProgressCount == 0; } } public string EmptyMessage { get { if (BlockedCount > 0) return "Active work is complete. " + BlockedCount + " task is still blocked."; if (DoneCount > 0) return "All clear. " + DoneCount + " tasks completed today."; return "You're clear for today. Add a task or tell Slave what needs to be done."; } }
        public string NewTitle { get { return newTitle; } set { if (SetProperty(ref newTitle, value)) CommandManager.InvalidateRequerySuggested(); } }
        public string NewDescription { get { return newDescription; } set { SetProperty(ref newDescription, value); } } public string NewCategory { get { return newCategory; } set { SetProperty(ref newCategory, value); } }
        public TaskPriority NewPriority { get { return newPriority; } set { SetProperty(ref newPriority, value); } } public DateTime? NewDueDate { get { return newDueDate; } set { SetProperty(ref newDueDate, value); } }
        public bool IsAddPanelOpen { get { return isAddPanelOpen; } set { SetProperty(ref isAddPanelOpen, value); } } public string ErrorMessage { get { return errorMessage; } set { SetProperty(ref errorMessage, value); } }
        public ICommand ToggleAddCommand { get; private set; } public ICommand QuickAddCommand { get; private set; } public ICommand AddTaskCommand { get; private set; } public ICommand StartCommand { get; private set; } public ICommand CompleteCommand { get; private set; } public ICommand BlockCommand { get; private set; } public ICommand ResumeCommand { get; private set; } public ICommand TodoCommand { get; private set; } public ICommand OpenTaskCommand { get; private set; } public ICommand DailySummaryCommand { get; private set; } public ICommand YesterdaySummaryCommand { get; private set; } public ICommand RefreshCommand { get; private set; }
        public async System.Threading.Tasks.Task LoadAsync() { if (displayedDate != clock.Now.Date) displayedDate = clock.Now.Date; var all = await service.GetAllAsync(); Tasks.Clear(); foreach (var task in all.OrderByDescending(x => x.UpdatedAt)) Tasks.Add(task); YesterdayCompleted.Clear(); DailySummary yesterday = await summaries.GenerateAsync(displayedDate.AddDays(-1)); foreach (WorkTask task in yesterday.Completed) YesterdayCompleted.Add(task); NotifyCollections(); }
        private async void Initialize() { await LoadAsync(); }
        private async System.Threading.Tasks.Task AddAsync() { try { ErrorMessage = null; await service.CreateTaskAsync(NewTitle, NewDescription, NewCategory, NewPriority, NewDueDate ?? displayedDate); NewTitle = NewDescription = ""; NewCategory = "기타"; NewPriority = TaskPriority.Normal; NewDueDate = null; IsAddPanelOpen = false; } catch (Exception ex) { ErrorMessage = ex.Message; } }
        private async System.Threading.Tasks.Task ChangeAsync(WorkTask task, Func<WorkTask, System.Threading.Tasks.Task> action) { if (task == null) return; try { ErrorMessage = null; await action(task); } catch (Exception ex) { ErrorMessage = ex.Message; } }
        private void NotifyCollections() { OnPropertyChanged("Greeting"); OnPropertyChanged("DateTitle"); OnPropertyChanged("InProgressTasks"); OnPropertyChanged("TodoTasks"); OnPropertyChanged("UpcomingTasks"); OnPropertyChanged("BacklogTasks"); OnPropertyChanged("CarryOverTasks"); OnPropertyChanged("BlockedTasks"); OnPropertyChanged("DoneTodayTasks"); OnPropertyChanged("TodoCount"); OnPropertyChanged("ProgressCount"); OnPropertyChanged("BlockedCount"); OnPropertyChanged("DoneCount"); OnPropertyChanged("YesterdayCount"); OnPropertyChanged("HasNoActiveWork"); OnPropertyChanged("EmptyMessage"); }
    }
}
