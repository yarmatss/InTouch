using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class MusicIndexViewModel
{
    public IEnumerable<Music> Music { get; set; } = new List<Music>();
    public string SortBy { get; set; } = "date"; // default sort - options: date, genre, artist, user
}
