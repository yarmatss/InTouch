using InTouch.MVC.Models;

namespace InTouch.MVC.Services;

public interface IMusicService
{
    Task<List<Music>> GetAllMusicAsync(string sortBy = "date");
    Task<List<Music>> GetUserMusicAsync(string userId);
    Task<Music> GetMusicByIdAsync(int id);
    Task<Music> UploadMusicAsync(string userId, string title, string artist, string description, 
        string genre, IFormFile musicFile, IFormFile coverFile = null);
    Task<bool> DeleteMusicAsync(int id, string userId);
}
