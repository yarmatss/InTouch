using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class UserMusicViewModel
{
    public ApplicationUser User { get; set; }
    public List<Music> Music { get; set; }
}
