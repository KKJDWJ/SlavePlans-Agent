using System;
namespace SlaveSplit.Services
{
    public sealed class DashboardNavigationService
    {
        public event EventHandler<DailySummaryNavigationEventArgs> DailySummaryRequested;
        public void OpenDailySummary(DateTime date) { EventHandler<DailySummaryNavigationEventArgs> handler = DailySummaryRequested; if (handler != null) handler(this, new DailySummaryNavigationEventArgs(date)); }
    }
    public sealed class DailySummaryNavigationEventArgs : EventArgs { public DailySummaryNavigationEventArgs(DateTime date) { Date = date.Date; } public DateTime Date { get; private set; } }
}
