using System.ComponentModel.DataAnnotations;

namespace InTouch.MVC.ViewModels;

public class EditProfileViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }
    
    [Required]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }
    
    [Display(Name = "Bio")]
    public string? Bio { get; set; }
    
    [Display(Name = "Profile Picture")]
    public IFormFile? ProfilePicture { get; set; }
    
    public string? CurrentProfilePicture { get; set; }
}