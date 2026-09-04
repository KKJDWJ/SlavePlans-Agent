using System;
namespace SlaveSplit.Services
{
    public sealed class TaskNavigationService
    {
        public event EventHandler<TaskNavigationEventArgs> OpenRequested; public event EventHandler BackRequested;
        public void Open(Guid taskId) { EventHandler<TaskNavigationEventArgs> handler = OpenRequested; if (handler != null) handler(this, new TaskNavigationEventArgs(taskId)); }
        public void Back() { EventHandler handler = BackRequested; if (handler != null) handler(this, EventArgs.Empty); }
    }
    public sealed class TaskNavigationEventArgs : EventArgs { public TaskNavigationEventArgs(Guid taskId) { TaskId = taskId; } public Guid TaskId { get; private set; } }
}
