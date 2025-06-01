using InTouch.MVC.Data;
using InTouch.MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InTouch.MVC.Services;

public class MessageService : IMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MessageService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<Message>> GetConversationAsync(string userId, string otherUserId)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                        (m.SenderId == otherUserId && m.ReceiverId == userId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<List<ApplicationUser>> GetConversationPartnersAsync(string userId)
    {
        // Find all users that the current user has exchanged messages with
        var sentMessages = await _context.Messages
            .Where(m => m.SenderId == userId)
            .Select(m => m.ReceiverId)
            .Distinct()
            .ToListAsync();

        var receivedMessages = await _context.Messages
            .Where(m => m.ReceiverId == userId)
            .Select(m => m.SenderId)
            .Distinct()
            .ToListAsync();

        var partnerIds = sentMessages.Union(receivedMessages).Distinct().ToList();

        return await _context.Users
            .Where(u => partnerIds.Contains(u.Id))
            .ToListAsync();
    }

    public async Task<Message> SendMessageAsync(string senderId, string receiverId, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        var message = new Message
        {
            Content = content,
            SenderId = senderId,
            ReceiverId = receiverId,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return message;
    }

    public async Task<bool> MarkAsReadAsync(int messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.ReceiverId != userId)
        {
            return false;
        }

        message.IsRead = true;
        _context.Update(message);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<DateTime> GetLastActivityAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.LastActive ?? DateTime.MinValue;
    }

    public async Task<Message?> GetLastMessageAsync(string otherUserId)
    {
        // Get the current user ID from the HttpContext
        var currentUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserId))
            return null;

        // Get the most recent message between the current user and the other user
        return await _context.Messages
            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                       (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
            .OrderByDescending(m => m.SentAt)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetUnreadCountAsync(string otherUserId)
    {
        // Get the current user ID from the HttpContext
        var currentUserId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserId))
            return 0;

        // Count unread messages sent from otherUserId to currentUserId
        return await _context.Messages
            .CountAsync(m => m.SenderId == otherUserId &&
                           m.ReceiverId == currentUserId &&
                           !m.IsRead);
    }
}