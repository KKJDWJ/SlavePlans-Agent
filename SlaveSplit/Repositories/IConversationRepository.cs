using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SlaveSplit.Models;
namespace SlaveSplit.Repositories
{
    public interface IConversationRepository
    {
        Task<IReadOnlyList<Conversation>> GetConversationsAsync();
        Task<Conversation> GetConversationAsync(Guid id);
        Task CreateConversationAsync(Conversation conversation);
        Task UpdateConversationAsync(Conversation conversation);
        Task DeleteConversationAsync(Guid id);
        Task<IReadOnlyList<Message>> GetMessagesAsync(Guid conversationId, int take = 100);
        Task AddMessageAsync(Message message);
        Task<Guid?> GetLastConversationIdAsync();
        Task SetLastConversationIdAsync(Guid id);
    }
}
