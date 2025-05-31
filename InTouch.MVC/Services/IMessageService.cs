using InTouch.MVC.Models;

namespace InTouch.MVC.Services;

public interface IMessageService
{
    Task<List<Message>> GetConversationAsync(string userId, string otherUserId);
    Task<List<ApplicationUser>> GetConversationPartnersAsync(string userId);
    Task<Message> SendMessageAsync(string senderId, string receiverId, string content);
    Task<bool> MarkAsReadAsync(int messageId, string userId);
    Task<DateTime> GetLastActivityAsync(string userId);
    Task<Message?> GetLastMessageAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}
