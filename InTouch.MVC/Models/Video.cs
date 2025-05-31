namespace InTouch.MVC.Models;

public class Video
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime UploadDate { get; set; }
    public int Views { get; set; }
    
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }
}