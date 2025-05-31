namespace InTouch.MVC.Models;

public class Friendship
{
    public int Id { get; set; }

    public string? RequesterId { get; set; }
    public virtual ApplicationUser? Requester { get; set; }

    public string? AddresseeId { get; set; }
    public virtual ApplicationUser Addressee { get; set; }

    public FriendshipStatusEnum Status { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? AcceptedDate { get; set; }
}