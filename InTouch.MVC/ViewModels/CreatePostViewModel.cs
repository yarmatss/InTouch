using System.ComponentModel.DataAnnotations;

namespace InTouch.MVC.ViewModels;

public class CreatePostViewModel
{
    [Required]
    [Display(Name = "What's on your mind?")]
    public string? Content { get; set; }
    
    [Display(Name = "Upload Image or Video")]
    public IFormFile? Media { get; set; }
}