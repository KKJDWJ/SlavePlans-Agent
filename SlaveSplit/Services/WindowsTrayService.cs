using System;
using System.Drawing;
using System.Reflection;
using System.Windows;
using Forms=System.Windows.Forms;
using SlaveSplit.Models;

namespace SlaveSplit.Services
{
    public sealed class WindowsTrayService : INotificationService,IDisposable
    {
        private readonly Forms.NotifyIcon icon;private readonly Window window;private bool exiting;
        public WindowsTrayService(Window window,Action showToday){this.window=window;Icon appIcon=null;try{appIcon=Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);}catch{}icon=new Forms.NotifyIcon{Icon=appIcon??SystemIcons.Application,Text="SlaveSplit",Visible=true};var menu=new Forms.ContextMenuStrip();menu.Items.Add("SlaveSplit 열기",null,(s,e)=>Show());menu.Items.Add("오늘 할 일",null,(s,e)=>{Show();if(showToday!=null)showToday();});menu.Items.Add("종료",null,(s,e)=>Exit());icon.ContextMenuStrip=menu;icon.DoubleClick+=(s,e)=>Show();window.Closing+=(s,e)=>{if(exiting)return;e.Cancel=true;window.Hide();};}
        public void ShowReminder(WorkTask task,string workspaceName,int minutes){icon.BalloonTipTitle="SlaveSplit";icon.BalloonTipText=(string.IsNullOrWhiteSpace(workspaceName)?"":"["+workspaceName+"]\n")+minutes+"분 뒤 예정된 업무가 있습니다.\n"+task.Title+"\n예정 시간: "+task.DueTime.Value.ToString(@"hh\:mm");icon.ShowBalloonTip(10000);}
        private void Show(){window.Show();if(window.WindowState==WindowState.Minimized)window.WindowState=WindowState.Normal;window.Activate();}
        private void Exit(){exiting=true;icon.Visible=false;window.Close();Application.Current.Shutdown();}
        public void Dispose(){icon.Visible=false;icon.Dispose();}
    }
}
