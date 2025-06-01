using System.ComponentModel.DataAnnotations;

namespace InTouch.MVC.ViewModels;

public class UploadVideoViewModel
{
    [Required]
    [Display(Name = "Title")]
    public string Title { get; set; }

    [Display(Name = "Description")]
    public string Description { get; set; }

    [Required]
    [Display(Name = "Video File")]
    public IFormFile VideoFile { get; set; }

    [Display(Name = "Thumbnail (Optional)")]
    public IFormFile? ThumbnailFile { get; set; }
}
