using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class UserVideosViewModel
{
    public ApplicationUser User { get; set; }
    public List<Video> Videos { get; set; }
}
