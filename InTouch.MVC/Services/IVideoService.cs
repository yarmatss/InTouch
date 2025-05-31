using InTouch.MVC.Models;

namespace InTouch.MVC.Services;

public interface IVideoService
{
    Task<List<Video>> GetAllVideosAsync(string sortBy = "date");
    Task<List<Video>> GetUserVideosAsync(string userId);
    Task<Video> GetVideoByIdAsync(int id);
    Task<Video> UploadVideoAsync(string userId, string title, string description, IFormFile videoFile, IFormFile thumbnailFile = null);
    Task<bool> DeleteVideoAsync(int id, string userId);
    Task<bool> IncrementViewCountAsync(int id);
}