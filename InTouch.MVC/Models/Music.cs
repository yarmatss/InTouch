namespace InTouch.MVC.Models;

public class Music
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Artist { get; set; }
    public string? MusicUrl { get; set; }
    public string? CoverUrl { get; set; }
    public string? Genre { get; set; }
    public DateTime UploadDate { get; set; }
    
    public string? UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }
}