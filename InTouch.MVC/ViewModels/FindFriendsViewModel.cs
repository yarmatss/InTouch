namespace InTouch.MVC.ViewModels;

public class FindFriendsViewModel
{
    public string? SearchTerm { get; set; }
    public List<UserWithFriendshipStatus> Results { get; set; } = new List<UserWithFriendshipStatus>();
}

