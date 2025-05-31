using InTouch.MVC.Models;
using InTouch.MVC.ViewModels;

namespace InTouch.MVC.Services;

public interface IFriendService
{
    Task<List<ApplicationUser>> GetFriendsAsync(string userId);
    Task<List<Friendship>> GetFriendRequestsAsync(string userId);
    Task<List<UserWithFriendshipStatus>> SearchUsersAsync(string userId, string searchTerm);
    Task<bool> SendFriendRequestAsync(string requesterId, string addresseeId);
    Task<bool> AcceptFriendRequestAsync(int friendshipId, string userId);
    Task<bool> RejectFriendRequestAsync(int friendshipId, string userId);
    Task<bool> RemoveFriendAsync(string userId, string friendId);
    Task<FriendshipStatusEnum> GetFriendshipStatusAsync(string userId, string otherUserId);
}
