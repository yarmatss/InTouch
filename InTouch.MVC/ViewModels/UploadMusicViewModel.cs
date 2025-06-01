using System.ComponentModel.DataAnnotations;

namespace InTouch.MVC.ViewModels;

public class UploadMusicViewModel
{
    [Display(Name = "Artist")]
    public string Artist { get; set; }

    [Required]
    [Display(Name = "Title")]
    public string Title { get; set; }

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Genre")]
    public string Genre { get; set; }

    [Required]
    [Display(Name = "Audio File")]
    public IFormFile MusicFile { get; set; }

    [Display(Name = "Cover Image (Optional)")]
    public IFormFile? CoverFile { get; set; }
}
