using InTouch.MVC.Data;
using InTouch.MVC.Models;
using InTouch.MVC.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InTouch.MVC.Services;

public class FriendService : IFriendService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FriendService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<ApplicationUser>> GetFriendsAsync(string userId)
    {
        // Get accepted friendships
        var friendships = await _context.Friendships
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) &&
                        f.Status == FriendshipStatusEnum.Accepted)
            .ToListAsync();

        // Extract friend users from friendships
        return friendships.Select(f =>
            f.RequesterId == userId ? f.Addressee : f.Requester).ToList();
    }

    public async Task<List<Friendship>> GetFriendRequestsAsync(string userId)
    {
        // Get pending friend requests sent to user
        return await _context.Friendships
            .Include(f => f.Requester)
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatusEnum.Pending)
            .ToListAsync();
    }

    public async Task<List<UserWithFriendshipStatus>> SearchUsersAsync(string userId, string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new List<UserWithFriendshipStatus>();
        }

        // Get users matching search term
        var users = await _userManager.Users
            .Where(u => u.Id != userId &&
                       (u.UserName.Contains(searchTerm) ||
                        u.FirstName.Contains(searchTerm) ||
                        u.LastName.Contains(searchTerm)))
            .ToListAsync();

        // Get existing friendships with these users
        var friendshipIds = users.Select(u => u.Id).ToList();
        var existingFriendships = await _context.Friendships
            .Where(f => (f.RequesterId == userId && friendshipIds.Contains(f.AddresseeId)) ||
                        (f.AddresseeId == userId && friendshipIds.Contains(f.RequesterId)))
            .ToListAsync();

        return users.Select(u => new UserWithFriendshipStatus
        {
            User = u,
            FriendshipStatus = GetFriendshipStatus(existingFriendships, userId, u.Id)
        }).ToList();
    }

    public async Task<bool> SendFriendRequestAsync(string requesterId, string addresseeId)
    {
        if (string.IsNullOrEmpty(addresseeId))
        {
            return false;
        }

        // Check if friendship already exists
        var existingFriendship = await _context.Friendships
            .FirstOrDefaultAsync(f => (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                                      (f.AddresseeId == requesterId && f.RequesterId == addresseeId));

        if (existingFriendship != null)
        {
            return false;
        }

        // Create new friendship request
        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatusEnum.Pending,
            RequestDate = DateTime.Now
        };

        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AcceptFriendRequestAsync(int friendshipId, string userId)
    {
        var friendship = await _context.Friendships.FindAsync(friendshipId);

        if (friendship == null || friendship.AddresseeId != userId)
        {
            return false;
        }

        // Accept friendship
        friendship.Status = FriendshipStatusEnum.Accepted;
        friendship.AcceptedDate = DateTime.Now;

        _context.Update(friendship);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectFriendRequestAsync(int friendshipId, string userId)
    {
        var friendship = await _context.Friendships.FindAsync(friendshipId);

        if (friendship == null || friendship.AddresseeId != userId)
        {
            return false;
        }

        // Delete friendship request
        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveFriendAsync(string userId, string friendId)
    {
        if (string.IsNullOrEmpty(friendId))
        {
            return false;
        }

        // Find friendship
        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => ((f.RequesterId == userId && f.AddresseeId == friendId) ||
                                      (f.AddresseeId == userId && f.RequesterId == friendId)) &&
                                      f.Status == FriendshipStatusEnum.Accepted);

        if (friendship == null)
        {
            return false;
        }

        // Remove friendship
        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();

        return true;
    }

    private FriendshipStatusEnum GetFriendshipStatus(List<Friendship> friendships, string userId, string otherUserId)
    {
        var friendship = friendships.FirstOrDefault(f =>
            (f.RequesterId == userId && f.AddresseeId == otherUserId) ||
            (f.AddresseeId == userId && f.RequesterId == otherUserId));

        if (friendship == null)
        {
            return FriendshipStatusEnum.None;
        }

        if (friendship.Status == FriendshipStatusEnum.Accepted)
        {
            return FriendshipStatusEnum.Friends;
        }

        if (friendship.RequesterId == userId)
        {
            return FriendshipStatusEnum.RequestSent;
        }

        return FriendshipStatusEnum.RequestReceived;
    }

    public async Task<FriendshipStatusEnum> GetFriendshipStatusAsync(string userId, string otherUserId)
    {
        // Find any friendship between these users
        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => (f.RequesterId == userId && f.AddresseeId == otherUserId) ||
                                      (f.RequesterId == otherUserId && f.AddresseeId == userId));

        if (friendship == null)
        {
            return FriendshipStatusEnum.None;
        }

        if (friendship.Status == FriendshipStatusEnum.Accepted)
        {
            return FriendshipStatusEnum.Friends;
        }

        // Check if current user is the requester
        if (friendship.RequesterId == userId)
        {
            return FriendshipStatusEnum.RequestSent;
        }

        return FriendshipStatusEnum.RequestReceived;
    }
}
