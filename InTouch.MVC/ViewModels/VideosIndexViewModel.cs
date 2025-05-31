using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class VideosIndexViewModel
{
    public List<Video> Videos { get; set; }
    public string SortBy { get; set; }
}
