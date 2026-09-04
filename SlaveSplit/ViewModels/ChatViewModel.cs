using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows;
using SlaveSplit.Commands;
using SlaveSplit.Models;
using SlaveSplit.Services;
using SlaveSplit.Agents;
using SlaveSplit.Services.AI;
namespace SlaveSplit.ViewModels
{
    public sealed class ChatViewModel : ViewModelBase
    {
        private string draftMessage;
        private readonly TaskService taskService; private readonly IAgentService agentService; private readonly ConversationService conversationService; private readonly AiProviderManager providerManager; private readonly TaskNavigationService taskNavigation; private int todoCount, progressCount, blockedCount, doneTodayCount; private bool isProcessing, isLoading; private Conversation currentConversation; private string copyStatus;
        public ChatViewModel(TaskService taskService, IAgentService agentService, ConversationService conversationService, AiProviderManager providerManager, TaskNavigationService taskNavigation)
        {
            this.taskService = taskService; this.agentService = agentService; this.conversationService = conversationService; this.providerManager = providerManager; this.taskNavigation = taskNavigation; this.taskService.TasksChanged += async (s, e) => await RefreshSummaryAsync(); this.providerManager.StatusChanged += (s, e) => { OnPropertyChanged("ProviderStatusText"); OnPropertyChanged("IsAiConnected"); OnPropertyChanged("ProviderStatusTooltip"); };
            Messages = new ObservableCollection<Message>();
            Messages.CollectionChanged += (s, e) => { OnPropertyChanged("IsEmpty"); CommandManager.InvalidateRequerySuggested(); };
            SendCommand = new RelayCommand(p => Send(), p => !string.IsNullOrWhiteSpace(DraftMessage) && !IsProcessing);
            NewChatCommand = new RelayCommand(async p => await NewChatAsync(), p => !IsProcessing); CopyChatCommand = new RelayCommand(p => CopyChat(), p => Messages.Count > 0); OpenRelatedTaskCommand = new RelayCommand(p => { Message message = p as Message; if (message != null && message.RelatedTaskId.HasValue) taskNavigation.Open(message.RelatedTaskId.Value); });
            Initialize();
        }
        public ObservableCollection<Message> Messages { get; private set; }
        public bool IsEmpty { get { return Messages.Count == 0 && !IsLoading; } }
        public ICommand SendCommand { get; private set; }
        public ICommand NewChatCommand { get; private set; }
        public ICommand CopyChatCommand { get; private set; }
        public ICommand OpenRelatedTaskCommand { get; private set; }
        public string DraftMessage { get { return draftMessage; } set { if (SetProperty(ref draftMessage, value)) CommandManager.InvalidateRequerySuggested(); } }
        public int TodoCount { get { return todoCount; } private set { SetProperty(ref todoCount, value); } }
        public int ProgressCount { get { return progressCount; } private set { SetProperty(ref progressCount, value); } }
        public int BlockedCount { get { return blockedCount; } private set { SetProperty(ref blockedCount, value); } }
        public int DoneTodayCount { get { return doneTodayCount; } private set { SetProperty(ref doneTodayCount, value); } }
        public bool IsProcessing { get { return isProcessing; } private set { if (SetProperty(ref isProcessing, value)) CommandManager.InvalidateRequerySuggested(); } }
        public bool IsLoading { get { return isLoading; } private set { if (SetProperty(ref isLoading, value)) OnPropertyChanged("IsEmpty"); } }
        public Conversation CurrentConversation { get { return currentConversation; } private set { if (SetProperty(ref currentConversation, value)) OnPropertyChanged("ConversationTitle"); } }
        public string ConversationTitle { get { return CurrentConversation == null ? "대화를 불러오는 중..." : CurrentConversation.Title; } }
        public bool IsAiConnected { get { return providerManager.Current.Status == AiProviderStatus.Connected; } }
        public string ProviderStatusText { get { return IsAiConnected ? "AI 연결됨" : "로컬 모드"; } }
        public string ProviderStatusTooltip { get { return IsAiConnected ? providerManager.Current.Name + " 사용 가능" : "Company AI를 사용할 수 없어 로컬 명령 모드를 사용합니다."; } }
        public string CopyStatus { get { return copyStatus; } private set { SetProperty(ref copyStatus, value); } }
        public string BuildCurrentTranscript() { return ChatTranscriptFormatter.Format(CurrentConversation, Messages); }
        private void CopyChat() { try { Clipboard.SetText(BuildCurrentTranscript()); CopyStatus = "대화를 클립보드에 복사했습니다."; } catch { CopyStatus = "대화를 복사하지 못했습니다."; } }
        private async System.Threading.Tasks.Task RefreshSummaryAsync() { var all = await taskService.GetAllAsync(); DateTime today = DateTime.Today; TodoCount = System.Linq.Enumerable.Count(all, x => x.Status == WorkTaskStatus.Todo); ProgressCount = System.Linq.Enumerable.Count(all, x => x.Status == WorkTaskStatus.InProgress); BlockedCount = System.Linq.Enumerable.Count(all, x => x.Status == WorkTaskStatus.Blocked); DoneTodayCount = System.Linq.Enumerable.Count(all, x => x.Status == WorkTaskStatus.Done && x.CompletedAt.HasValue && x.CompletedAt.Value.Date == today); }
        private async void Initialize() { await RefreshSummaryAsync(); await LoadInitialConversationAsync(); }
        private async System.Threading.Tasks.Task LoadInitialConversationAsync() { IsLoading = true; try { CurrentConversation = await conversationService.GetInitialConversationAsync(); await LoadMessagesAsync(); agentService.ResetContext(); } finally { IsLoading = false; } }
        private async System.Threading.Tasks.Task LoadMessagesAsync() { Messages.Clear(); if (CurrentConversation == null) return; var stored = await conversationService.GetMessagesAsync(CurrentConversation.Id, 100); foreach (Message message in stored) Messages.Add(message); }
        private async System.Threading.Tasks.Task NewChatAsync() { CurrentConversation = await conversationService.CreateConversationAsync(true); Messages.Clear(); CopyStatus = null; agentService.ResetContext(); }
        private async void Send()
        {
            string content = DraftMessage.Trim();
            DraftMessage = string.Empty;
            IsProcessing = true;
            try
            {
                Conversation daily = await conversationService.EnsureDailyConversationAsync(CurrentConversation); if (CurrentConversation == null || daily.Id != CurrentConversation.Id) { CurrentConversation = daily; Messages.Clear(); agentService.ResetContext(); }
                Message userMessage = new Message { Id = Guid.NewGuid(), ConversationId = CurrentConversation.Id, Sender = "Me", Content = content, CreatedAt = DateTime.Now, MessageType = MessageType.User };
                Messages.Add(userMessage); await conversationService.AddMessageAsync(CurrentConversation, userMessage);
                AgentResponse response = await agentService.ProcessAsync(new AgentRequest { Message = content, CreatedAt = DateTime.Now, ConversationMessages = Messages.ToList() });
                if (response.ClearConversation) { Guid oldConversationId = CurrentConversation.Id; await conversationService.DeleteConversationAsync(oldConversationId); CurrentConversation = await conversationService.CreateConversationAsync(true); Messages.Clear(); agentService.ResetContext(); }
                Message assistantMessage = new Message { Id = Guid.NewGuid(), ConversationId = CurrentConversation.Id, Sender = "Slave", Content = response.Message, CreatedAt = DateTime.Now, MessageType = response.Success ? MessageType.Assistant : MessageType.System, RelatedTaskId = response.RelatedTaskId };
                Messages.Add(assistantMessage); await conversationService.AddMessageAsync(CurrentConversation, assistantMessage);
            }
            catch
            {
                Message error = new Message { Id = Guid.NewGuid(), ConversationId = CurrentConversation == null ? Guid.Empty : CurrentConversation.Id, Sender = "Slave", Content = "업무 처리 중 문제가 발생했습니다.\n\n자세한 내용은 로그를 확인해주세요.", CreatedAt = DateTime.Now, MessageType = MessageType.System };
                Messages.Add(error); if (CurrentConversation != null) { try { await conversationService.AddMessageAsync(CurrentConversation, error); } catch { } }
            }
            finally { IsProcessing = false; }
        }
    }
}
