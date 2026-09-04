using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SlaveSplit.ViewModels;
namespace SlaveSplit.Views
{
    public partial class ChatView : UserControl
    {
        private bool followLatest = true;
        public ChatView() { InitializeComponent(); Loaded += OnLoaded; }
        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ChatViewModel vm = DataContext as ChatViewModel;
            if (vm != null) { vm.Messages.CollectionChanged -= OnMessagesChanged; vm.Messages.CollectionChanged += OnMessagesChanged; ScrollToLatest(); }
        }
        private void OnMessagesChanged(object sender, NotifyCollectionChangedEventArgs e) { if(followLatest)Dispatcher.BeginInvoke(new System.Action(ScrollToLatest)); }
        private void OnMessageListMouseWheel(object sender,MouseWheelEventArgs e){ScrollViewer scroll=FindScrollViewer(MessageList);if(scroll==null)return;followLatest=e.Delta>0?false:scroll.ScrollableHeight-scroll.VerticalOffset<=80;}
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox input = e.OriginalSource as TextBox; Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (input == null || key != Key.Enter || (Keyboard.Modifiers & (ModifierKeys.Shift | ModifierKeys.Alt)) == ModifierKeys.None) return;
            int start = input.SelectionStart; string text = input.Text ?? ""; input.Text = text.Remove(start, input.SelectionLength).Insert(start, System.Environment.NewLine); input.SelectionStart = start + System.Environment.NewLine.Length; input.SelectionLength = 0; e.Handled = true;
        }
        private void ScrollToLatest() { ScrollViewer scroll=FindScrollViewer(MessageList);if(scroll!=null)scroll.ScrollToEnd();else if (MessageList.Items.Count > 0) MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);followLatest=true; }
        private static ScrollViewer FindScrollViewer(DependencyObject value){if(value==null)return null;if(value is ScrollViewer)return (ScrollViewer)value;for(int i=0;i<VisualTreeHelper.GetChildrenCount(value);i++){ScrollViewer found=FindScrollViewer(VisualTreeHelper.GetChild(value,i));if(found!=null)return found;}return null;}
    }
}
