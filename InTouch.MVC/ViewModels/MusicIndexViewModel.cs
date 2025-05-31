using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class MusicIndexViewModel
{
    public List<Music> Music { get; set; }
    public string SortBy { get; set; }
}
