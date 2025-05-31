namespace InTouch.MVC.Models;

using Microsoft.AspNetCore.Identity;
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Bio { get; set; }
    public string ProfilePicture { get; set; }
    public DateTime LastActive { get; set; }

    // Navigation properties
    public virtual ICollection<Friendship> SentFriendRequests { get; set; }
    public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; }
    public virtual ICollection<Post> Posts { get; set; }
    public virtual ICollection<Comment> Comments { get; set; }
    public virtual ICollection<Like> Likes { get; set; }
    public virtual ICollection<Message> SentMessages { get; set; }
    public virtual ICollection<Message> ReceivedMessages { get; set; }
    public virtual ICollection<Video> Videos { get; set; }
    public virtual ICollection<Music> Musics { get; set; }

    public ApplicationUser()
    {
        SentFriendRequests = new HashSet<Friendship>();
        ReceivedFriendRequests = new HashSet<Friendship>();
        Posts = new HashSet<Post>();
        Comments = new HashSet<Comment>();
        Likes = new HashSet<Like>();
        SentMessages = new HashSet<Message>();
        ReceivedMessages = new HashSet<Message>();
        Videos = new HashSet<Video>();
        Musics = new HashSet<Music>();
    }
}