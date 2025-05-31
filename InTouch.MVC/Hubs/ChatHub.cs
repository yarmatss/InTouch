using InTouch.MVC.Data;
using InTouch.MVC.Hubs.Utils;
using InTouch.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
        var userId = Context.UserIdentifier; // Use UserIdentifier rather than Identity.Name

        if (userId != null)
        {
            _connections.Add(userId, Context.ConnectionId);

            // Update user's last active status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActive = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            await Clients.Others.SendAsync("UserConnected", userId);
        }
        else
        {
            Console.WriteLine("Warning: Could not identify user for SignalR connection");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;

        if (userId != null)
        {
            _connections.Remove(userId, Context.ConnectionId);

            // Update user's last active status
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActive = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            await Clients.Others.SendAsync("UserDisconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string message)
    {
        try
        {
            // Validate parameters
            if (string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(message))
            {
                // Early exit with no error for empty messages
                return;
            }

            // Get sender ID from the authenticated user
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(senderId))
            {
                // Try to get from Identity.Name as fallback
                senderId = Context.User?.Identity?.Name;

                if (string.IsNullOrEmpty(senderId))
                {
                    throw new HubException("User is not authenticated properly");
                }
            }

            // Save message to database
            var newMessage = new Message
            {
                Content = message,
                SenderId = senderId,
                ReceiverId = receiverId,
                SentAt = DateTime.Now,
                IsRead = false
            };

            await _context.Messages.AddAsync(newMessage);
            await _context.SaveChangesAsync();

            // Try to send directly to connected clients of this user
            var receiverConnections = _connections.GetConnections(receiverId);
            if (receiverConnections.Any())
            {
                await Clients.Clients(receiverConnections.ToList()).SendAsync("ReceiveMessage",
                    newMessage.Id, senderId, message, newMessage.SentAt);
            }
            else
            {
                // Fallback to standard SignalR user mapping
                await Clients.User(receiverId).SendAsync("ReceiveMessage",
                    newMessage.Id, senderId, message, newMessage.SentAt);
            }

            // Send to sender for UI update confirmation
            await Clients.Caller.SendAsync("MessageSent",
                newMessage.Id, message, newMessage.SentAt);
        }
        catch (Exception ex)
        {
            // Log the error (replace with your logging system)
            Console.WriteLine($"Error in SendMessage: {ex.Message}");

            // Properly communicate the error back to the client
            throw new HubException($"Error sending message: {ex.Message}");
        }
    }

    public async Task TypingStart(string receiverId)
    {
        var senderId = Context.UserIdentifier ?? Context.User?.Identity?.Name;

        if (!string.IsNullOrEmpty(senderId) && !string.IsNullOrEmpty(receiverId))
        {
            // Try direct connections first
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
    }

    public async Task TypingStop(string receiverId)
    {
        var senderId = Context.UserIdentifier ?? Context.User?.Identity?.Name;

        if (!string.IsNullOrEmpty(senderId) && !string.IsNullOrEmpty(receiverId))
        {
            // Try direct connections first
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
    }

    public async Task MarkAsRead(int messageId)
    {
        var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name;

        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("User not authenticated");
        }

        var message = await _context.Messages.FindAsync(messageId);
        if (message != null && message.ReceiverId == userId)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();

            // Notify the sender that their message was read
            var senderConnections = _connections.GetConnections(message.SenderId);
            if (senderConnections.Any())
            {
                await Clients.Clients(senderConnections.ToList()).SendAsync("MessageRead", messageId);
            }
            else
            {
                await Clients.User(message.SenderId).SendAsync("MessageRead", messageId);
            }
        }
    }
}