namespace InTouch.MVC.Models;

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; }
    public string? MediaUrl { get; set; }
    public MediaType? MediaType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string UserId { get; set; }
    public virtual ApplicationUser User { get; set; }

    public virtual ICollection<Comment> Comments { get; set; }
    public virtual ICollection<Like> Likes { get; set; }

    public Post()
    {
        Comments = new HashSet<Comment>();
        Likes = new HashSet<Like>();
    }
}

public enum MediaType
{
    Image,
    Video
}
