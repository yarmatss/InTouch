using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class UserWithFriendshipStatus
{
    public ApplicationUser? User { get; set; }
    public FriendshipStatusEnum FriendshipStatus { get; set; }
}
