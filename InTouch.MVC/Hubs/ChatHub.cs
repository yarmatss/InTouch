using InTouch.MVC.Data;
using InTouch.MVC.Hubs.Utils;
using InTouch.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // Required for CountAsync

namespace InTouch.MVC.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private static readonly ConnectionMapping<string> _connections = new ConnectionMapping<string>();

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (userId != null)
        {
            _connections.Add(userId, Context.ConnectionId);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActive = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await Clients.Others.SendAsync("UserConnected", userId);
        }
        else
        {
            // Consider using a logger here
            Console.WriteLine("Warning: Could not identify user for SignalR connection in OnConnectedAsync.");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;

        if (userId != null)
        {
            _connections.Remove(userId, Context.ConnectionId);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActive = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await Clients.Others.SendAsync("UserDisconnected", userId);
        }
        else
        {
            // Consider using a logger here
            Console.WriteLine("Warning: Could not identify user for SignalR connection in OnDisconnectedAsync.");
        }


        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string messageContent) // Renamed 'message' to 'messageContent' for clarity
    {
        try
        {
            if (string.IsNullOrEmpty(receiverId) || string.IsNullOrWhiteSpace(messageContent))
            {
                return; // Early exit for empty or whitespace-only messages
            }

            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId))
            {
                // This should ideally not happen if [Authorize] is effective
                throw new HubException("User is not authenticated properly.");
            }

            var newMessage = new Message
            {
                Content = messageContent,
                SenderId = senderId,
                ReceiverId = receiverId,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _context.Messages.AddAsync(newMessage);
            await _context.SaveChangesAsync();

            // Notify receiver about the new message for their active chat window
            var receiverConnections = _connections.GetConnections(receiverId);
            if (receiverConnections.Any())
            {
                await Clients.Clients(receiverConnections.ToList()).SendAsync("ReceiveMessage",
                    newMessage.Id, senderId, messageContent, newMessage.SentAt);
            }
            else
            {
                // Fallback if direct connection ID is not found (e.g., user connected via different server instance in a scaled-out scenario without backplane)
                await Clients.User(receiverId).SendAsync("ReceiveMessage",
                    newMessage.Id, senderId, messageContent, newMessage.SentAt);
            }

            // Confirm to sender that message was sent (for their active chat window)
            await Clients.Caller.SendAsync("MessageSent",
                newMessage.Id, messageContent, newMessage.SentAt);

            // --- New logic for updating conversation lists ---

            // Update sender's conversation list
            // Unread count for sender is messages they received from receiverId and haven't read
            int unreadBySenderFromReceiver = await _context.Messages
                .CountAsync(m => m.SenderId == receiverId && m.ReceiverId == senderId && !m.IsRead);
            await Clients.Caller.SendAsync("UpdateConversationList",
                receiverId, // The other user in this conversation
                messageContent,
                newMessage.SentAt,
                true, // Indicates the caller is the sender of this last message
                unreadBySenderFromReceiver);

            // Update receiver's conversation list
            // Unread count for receiver is messages they received from senderId and haven't read
            int unreadByReceiverFromSender = await _context.Messages
                .CountAsync(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead);

            if (receiverConnections.Any())
            {
                await Clients.Clients(receiverConnections.ToList()).SendAsync("UpdateConversationList",
                    senderId, // The other user in this conversation
                    messageContent,
                    newMessage.SentAt,
                    false, // Indicates the caller is NOT the sender of this last message (they are receiving)
                    unreadByReceiverFromSender);
            }
            else
            {
                await Clients.User(receiverId).SendAsync("UpdateConversationList",
                   senderId,
                   messageContent,
                   newMessage.SentAt,
                   false,
                   unreadByReceiverFromSender);
            }
        }
        catch (Exception ex)
        {
            // Replace with your actual logging mechanism (e.g., ILogger)
            Console.WriteLine($"Error in SendMessage: {ex.Message} \nStackTrace: {ex.StackTrace}");
            throw new HubException($"An error occurred while sending the message. Please try again.");
        }
    }

    public async Task TypingStart(string receiverId)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId)) return;

        var receiverConnections = _connections.GetConnections(receiverId);
        if (receiverConnections.Any())
        {
            await Clients.Clients(receiverConnections.ToList()).SendAsync("UserTyping", senderId, true);
        }
        else
        {
            await Clients.User(receiverId).SendAsync("UserTyping", senderId, true);
        }
    }

    public async Task TypingStop(string receiverId)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId)) return;

        var receiverConnections = _connections.GetConnections(receiverId);
        if (receiverConnections.Any())
        {
            await Clients.Clients(receiverConnections.ToList()).SendAsync("UserTyping", senderId, false);
        }
        else
        {
            await Clients.User(receiverId).SendAsync("UserTyping", senderId, false);
        }
    }

    public async Task MarkAsRead(int messageId)
    {
        var readerUserId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(readerUserId))
        {
            throw new HubException("User not authenticated.");
        }

        var message = await _context.Messages.FindAsync(messageId);
        if (message == null)
        {
            // Optionally, log this or inform client if message not found
            return;
        }

        if (message.ReceiverId == readerUserId && !message.IsRead) // Process only if reader is receiver and message isn't already read
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();

            // Notify the original sender that their message was read (for active chat window)
            var originalSenderConnections = _connections.GetConnections(message.SenderId);
            if (originalSenderConnections.Any())
            {
                await Clients.Clients(originalSenderConnections.ToList()).SendAsync("MessageRead", messageId);
            }
            else
            {
                await Clients.User(message.SenderId).SendAsync("MessageRead", messageId);
            }

            // --- New logic for clearing unread count in conversation list ---
            // Notify the reader (caller) to update their conversation list for the partner whose message they just read
            await Clients.Caller.SendAsync("ClearUnreadCountForConversation", message.SenderId);
        }
    }

    public async Task UpdateLastActive()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActive = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                // Consider if "UserActive" event is handled by ConversationsListUI for online status
                await Clients.Others.SendAsync("UserActive", userId, DateTime.UtcNow);
            }
        }
    }
}