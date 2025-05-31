using System.ComponentModel.DataAnnotations;

namespace InTouch.MVC.ViewModels;

public class EditPostViewModel
{
    public int Id { get; set; }
    
    [Required]
    public string? Content { get; set; }
}