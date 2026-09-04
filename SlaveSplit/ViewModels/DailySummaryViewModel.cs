using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SlaveSplit.Commands;
using SlaveSplit.Infrastructure;
using SlaveSplit.Models;
using SlaveSplit.Services;
namespace SlaveSplit.ViewModels
{
    public sealed class DailySummaryViewModel : ViewModelBase
    {
        private readonly DailySummaryService service; private readonly IClock clock; private readonly DateTime date; private DailySummary summary; private string summaryText, copyStatus;
        public DailySummaryViewModel(DailySummaryService service, IClock clock = null, DateTime? date = null) { this.service = service; this.clock = clock ?? new SystemClock(); this.date = (date ?? this.clock.Now.Date).Date; Completed = new ObservableCollection<WorkTask>(); Remaining = new ObservableCollection<WorkTask>(); Blocked = new ObservableCollection<WorkTask>(); CopyCommand = new RelayCommand(p => Copy()); RefreshCommand = new RelayCommand(async p => await LoadAsync()); Initialize(); }
        public DailySummary Summary { get { return summary; } private set { SetProperty(ref summary, value); NotifySummary(); } } public ObservableCollection<WorkTask> Completed { get; private set; } public ObservableCollection<WorkTask> Remaining { get; private set; } public ObservableCollection<WorkTask> Blocked { get; private set; }
        public string DateText { get { return Summary == null ? "" : Summary.Date.ToString("MMMM d, yyyy"); } } public string SummaryText { get { return summaryText; } private set { SetProperty(ref summaryText, value); } } public string CopyStatus { get { return copyStatus; } private set { SetProperty(ref copyStatus, value); } }
        public ICommand CopyCommand { get; private set; } public ICommand RefreshCommand { get; private set; }
        public async System.Threading.Tasks.Task LoadAsync() { Summary = await service.GenerateAsync(date); SummaryText = await service.GenerateTextAsync(date); Completed.Clear(); foreach (WorkTask item in Summary.Completed) Completed.Add(item); Remaining.Clear(); foreach (WorkTask item in Summary.Remaining) if (item.Status != WorkTaskStatus.Blocked) Remaining.Add(item); Blocked.Clear(); foreach (WorkTask item in Summary.Blocked) Blocked.Add(item); }
        private async void Initialize() { await LoadAsync(); }
        private void Copy() { try { Clipboard.SetText(SummaryText ?? ""); CopyStatus = "Copied to clipboard"; } catch (Exception ex) { CopyStatus = "Copy failed: " + ex.Message; } }
        private void NotifySummary() { OnPropertyChanged("DateText"); }
    }
}
