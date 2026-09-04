using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SlaveSplit.Models;
using SlaveSplit.ViewModels;

namespace SlaveSplit.Views
{
    public partial class TaskBoardView : UserControl
    {
        private Point dragStart; private WorkTask draggedTask; private bool didDrag;
        public TaskBoardView() { InitializeComponent(); }
        private void CardMouseDown(object sender, MouseButtonEventArgs e) { didDrag=false;dragStart = e.GetPosition(this); draggedTask = ((FrameworkElement)sender).DataContext as WorkTask; }
        private void CardMouseUp(object sender,MouseButtonEventArgs e){WorkTask task=((FrameworkElement)sender).DataContext as WorkTask;if(!didDrag&&task!=null)((TaskBoardViewModel)DataContext).OpenTaskCommand.Execute(task);draggedTask=null;}
        private void CardMouseMove(object sender, MouseEventArgs e) { if (e.LeftButton != MouseButtonState.Pressed || draggedTask == null) return; Point p = e.GetPosition(this); if (Math.Abs(p.X - dragStart.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(p.Y - dragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return; didDrag=true;Border card=sender as Border;System.Windows.Media.Brush original=card==null?null:card.Background;if(card!=null)card.Background=(System.Windows.Media.Brush)FindResource("ControlSelected");DragDrop.DoDragDrop((DependencyObject)sender, draggedTask, DragDropEffects.Move);if(card!=null)card.Background=original;draggedTask = null; }
        private async void ColumnDrop(object sender, DragEventArgs e) { Border border=sender as Border;WorkTask task=e.Data.GetData(typeof(WorkTask)) as WorkTask;WorkTaskStatus target;if(task!=null&&border!=null&&Enum.TryParse(Convert.ToString(border.Tag),out target))await ((TaskBoardViewModel)DataContext).MoveAsync(task,target); }
    }
}
