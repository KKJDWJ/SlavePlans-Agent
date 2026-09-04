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
    public sealed class WeeklyReportViewModel : ViewModelBase
    {
        private readonly WeeklySummaryService service; private readonly WorkReportFormatter formatter; private readonly IClock clock; private DateRange range; private WeeklySummary summary; private DateTime selectedDate; private ReportFormat selectedFormat; private string reportText, copyStatus, keyword;
        public WeeklyReportViewModel(WeeklySummaryService service, WorkReportFormatter formatter, IClock clock = null) { this.service = service; this.formatter = formatter; this.clock = clock ?? new SystemClock(); range = service.GetWeek(this.clock.Now.Date); selectedDate = this.clock.Now.Date; DailyGroups = new ObservableCollection<DailyTaskGroup>(); Formats = new[] { ReportFormat.Detailed, ReportFormat.Compact }; PreviousCommand = new RelayCommand(async p => { range = Shift(range, -7); await LoadAsync(); }); NextCommand = new RelayCommand(async p => { range = Shift(range, 7); await LoadAsync(); }); ThisWeekCommand = new RelayCommand(async p => { range = service.GetWeek(this.clock.Now.Date); await LoadAsync(); }); CustomDateCommand = new RelayCommand(async p => await LoadSelectedDateAsync()); SearchCommand = new RelayCommand(async p => await LoadAsync()); CopyCommand = new RelayCommand(p => Copy()); Initialize(); }
        public WeeklySummary Summary { get { return summary; } private set { SetProperty(ref summary, value); OnPropertyChanged("RangeText"); OnPropertyChanged("CompletionText"); } } public ObservableCollection<DailyTaskGroup> DailyGroups { get; private set; } public ReportFormat[] Formats { get; private set; }
        public DateTime SelectedDate { get { return selectedDate; } set { if (SetProperty(ref selectedDate, value)) LoadSelectedDate(); } } public ReportFormat SelectedFormat { get { return selectedFormat; } set { if (SetProperty(ref selectedFormat, value)) RefreshReport(); } } public string Keyword { get { return keyword; } set { SetProperty(ref keyword, value); } }
        public string RangeText { get { return Summary == null ? "" : Summary.StartDate.ToString("MMM d") + " — " + Summary.EndDate.ToString("MMM d, yyyy"); } } public string CompletionText { get { return Summary == null ? "" : Summary.CompletedCount + " / " + Summary.TotalCount + " completed"; } } public string ReportText { get { return reportText; } private set { SetProperty(ref reportText, value); } } public string CopyStatus { get { return copyStatus; } private set { SetProperty(ref copyStatus, value); } }
        public ICommand PreviousCommand { get; private set; } public ICommand NextCommand { get; private set; } public ICommand ThisWeekCommand { get; private set; } public ICommand CustomDateCommand { get; private set; } public ICommand SearchCommand { get; private set; } public ICommand CopyCommand { get; private set; }
        public async System.Threading.Tasks.Task LoadAsync() { Summary = await service.GenerateAsync(range, Keyword); DailyGroups.Clear(); foreach (DailyTaskGroup group in Summary.DailySummaries) if (group.Tasks.Count > 0) DailyGroups.Add(group); ReportText = await formatter.FormatAsync(Summary, SelectedFormat); }
        private async System.Threading.Tasks.Task LoadSelectedDateAsync() { range = new DateRange { StartDate = SelectedDate.Date, EndDate = SelectedDate.Date }; await LoadAsync(); }
        private async void LoadSelectedDate() { await LoadSelectedDateAsync(); }
        private async void Initialize() { await LoadAsync(); } private async void RefreshReport() { if (Summary != null) ReportText = await formatter.FormatAsync(Summary, SelectedFormat); }
        private void Copy() { try { Clipboard.SetText(ReportText ?? ""); CopyStatus = "Copied"; } catch (Exception ex) { CopyStatus = "Copy failed: " + ex.Message; } }
        private static DateRange Shift(DateRange value, int days) { return new DateRange { StartDate = value.StartDate.AddDays(days), EndDate = value.EndDate.AddDays(days) }; }
    }
}
