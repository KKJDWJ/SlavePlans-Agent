using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SlaveSplit.Commands;
using SlaveSplit.Models;
using SlaveSplit.Services;
namespace SlaveSplit.ViewModels
{
    public sealed class ConversationSummaryViewModel
    {
        public Conversation Conversation { get; set; } public int MessageCount { get; set; } public string LastMessage { get; set; }
        public Guid Id { get { return Conversation.Id; } } public string Title { get { return Conversation.Title; } } public string DateText { get { return Conversation.CreatedAt.ToString("yyyy.MM.dd"); } } public string MetaText { get { return MessageCount + " messages · " + Conversation.LastMessageAt.ToString("HH:mm"); } }
    }
    public sealed class HistoryViewModel : ViewModelBase
    {
        private readonly ConversationService service; private readonly TaskNavigationService navigation; private ConversationSummaryViewModel selectedConversation, pendingDelete; private string searchText; private bool isLoading;
        public HistoryViewModel(ConversationService service, TaskNavigationService navigation)
        {
            this.service = service; this.navigation = navigation; Conversations = new ObservableCollection<ConversationSummaryViewModel>(); Messages = new ObservableCollection<Message>(); SearchCommand = new RelayCommand(async p => await LoadAsync(SearchText)); ClearSearchCommand = new RelayCommand(async p => { SearchText = ""; await LoadAsync(null); }); RequestDeleteCommand = new RelayCommand(p => PendingDelete = p as ConversationSummaryViewModel); CancelDeleteCommand = new RelayCommand(p => PendingDelete = null); ConfirmDeleteCommand = new RelayCommand(async p => await DeleteAsync()); OpenRelatedTaskCommand = new RelayCommand(p => { Message message = p as Message; if (message != null && message.RelatedTaskId.HasValue) navigation.Open(message.RelatedTaskId.Value); }); service.ConversationsChanged += async (s, e) => await LoadAsync(SearchText); Initialize();
        }
        public ObservableCollection<ConversationSummaryViewModel> Conversations { get; private set; } public ObservableCollection<Message> Messages { get; private set; }
        public string SearchText { get { return searchText; } set { SetProperty(ref searchText, value); } }
        public bool IsLoading { get { return isLoading; } private set { SetProperty(ref isLoading, value); } }
        public ConversationSummaryViewModel SelectedConversation { get { return selectedConversation; } set { if (SetProperty(ref selectedConversation, value)) { OnPropertyChanged("HasSelectedConversation"); LoadSelected(); } } }
        public bool HasSelectedConversation { get { return SelectedConversation != null; } }
        public ConversationSummaryViewModel PendingDelete { get { return pendingDelete; } private set { SetProperty(ref pendingDelete, value); OnPropertyChanged("IsDeleteConfirmationVisible"); } }
        public bool IsDeleteConfirmationVisible { get { return PendingDelete != null; } }
        public ICommand SearchCommand { get; private set; } public ICommand ClearSearchCommand { get; private set; } public ICommand RequestDeleteCommand { get; private set; } public ICommand CancelDeleteCommand { get; private set; } public ICommand ConfirmDeleteCommand { get; private set; } public ICommand OpenRelatedTaskCommand { get; private set; }
        private async void Initialize() { await LoadAsync(null); }
        private async System.Threading.Tasks.Task LoadAsync(string query)
        {
            IsLoading = true; try { var items = await service.SearchAsync(query); Guid? selectedId = SelectedConversation == null ? (Guid?)null : SelectedConversation.Id; Conversations.Clear(); foreach (Conversation item in items) { var messages = await service.GetMessagesAsync(item.Id, 100); Message last = messages.LastOrDefault(); Conversations.Add(new ConversationSummaryViewModel { Conversation = item, MessageCount = messages.Count, LastMessage = last == null ? "No messages yet" : last.Content.Replace(Environment.NewLine, " ") }); } SelectedConversation = selectedId.HasValue ? Conversations.FirstOrDefault(x => x.Id == selectedId.Value) : Conversations.FirstOrDefault(); } finally { IsLoading = false; }
        }
        private async void LoadSelected() { Messages.Clear(); if (SelectedConversation == null) return; var items = await service.GetMessagesAsync(SelectedConversation.Id, 100); foreach (Message item in items) Messages.Add(item); }
        private async System.Threading.Tasks.Task DeleteAsync() { if (PendingDelete == null) return; Guid id = PendingDelete.Id; PendingDelete = null; await service.DeleteConversationAsync(id); if (SelectedConversation != null && SelectedConversation.Id == id) SelectedConversation = null; await LoadAsync(SearchText); }
    }
}
