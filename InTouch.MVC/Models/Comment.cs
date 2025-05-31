namespace InTouch.MVC.Models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int PostId { get; set; }
    public virtual Post Post { get; set; }

    public string UserId { get; set; }
    public virtual ApplicationUser User { get; set; }
}
