using System.Windows;
using SlaveSplit.ViewModels;
using SlaveSplit.Infrastructure;
using SlaveSplit.Repositories;
using SlaveSplit.Services;
using SlaveSplit.Services.GitHub;
namespace SlaveSplit { public partial class MainWindow : Window { private WindowsTrayService tray;private ReminderService reminders; public MainWindow() { InitializeComponent();MainViewModel vm=new MainViewModel(); DataContext = vm;SystemClock clock=new SystemClock();WorkspaceService workspaces=new WorkspaceService(null,new GitHubSettingsService().Load());TaskService tasks=new TaskService(new WorkspaceTaskRepository(new LocalTaskRepository(),workspaces),clock);tray=new WindowsTrayService(this,()=>vm.NavigateCommand.Execute("Today"));reminders=new ReminderService(tasks,workspaces,clock,tray);Closed+=(s,e)=>{reminders.Dispose();tray.Dispose();}; } } }
