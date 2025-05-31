using InTouch.MVC.Data;
using InTouch.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace InTouch.MVC.Services;

public class VideoService : IVideoService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public VideoService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<List<Video>> GetAllVideosAsync(string sortBy = "date")
    {
        IQueryable<Video> query = _context.Videos.Include(v => v.User);

        if (sortBy == "popularity")
        {
            return await query.OrderByDescending(v => v.Views).ToListAsync();
        }
        else // Default: date
        {
            return await query.OrderByDescending(v => v.UploadDate).ToListAsync();
        }
    }

    public async Task<List<Video>> GetUserVideosAsync(string userId)
    {
        return await _context.Videos
            .Include(v => v.User)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.UploadDate)
            .ToListAsync();
    }

    public async Task<Video> GetVideoByIdAsync(int id)
    {
        return await _context.Videos
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Video> UploadVideoAsync(string userId, string title, string description, IFormFile videoFile, IFormFile thumbnailFile = null)
    {
        if (videoFile == null || videoFile.Length == 0)
        {
            return null;
        }

        // Validate file is a video
        if (!videoFile.ContentType.StartsWith("video/"))
        {
            return null;
        }

        // Save video file
        string videoUrl = await _fileStorage.SaveFile(videoFile, "videos");

        // Save thumbnail if provided, otherwise generate one (simplified)
        string thumbnailUrl = null;
        if (thumbnailFile != null && thumbnailFile.Length > 0)
        {
            thumbnailUrl = await _fileStorage.SaveFile(thumbnailFile, "thumbnails");
        }
        else
        {
            // In real app, would generate a thumbnail from the video
            // For now, use a placeholder
            thumbnailUrl = "/images/default-video-thumbnail.jpg";
        }

        var video = new Video
        {
            Title = title,
            Description = description,
            VideoUrl = videoUrl,
            ThumbnailUrl = thumbnailUrl,
            UploadDate = DateTime.Now,
            Views = 0,
            UserId = userId
        };

        _context.Videos.Add(video);
        await _context.SaveChangesAsync();

        return video;
    }

    public async Task<bool> DeleteVideoAsync(int id, string userId)
    {
        var video = await _context.Videos.FindAsync(id);

        if (video == null || video.UserId != userId)
        {
            return false;
        }

        // Delete files
        if (!string.IsNullOrEmpty(video.VideoUrl))
        {
            _fileStorage.DeleteFile(video.VideoUrl);
        }

        if (!string.IsNullOrEmpty(video.ThumbnailUrl) &&
            !video.ThumbnailUrl.EndsWith("default-video-thumbnail.jpg"))
        {
            _fileStorage.DeleteFile(video.ThumbnailUrl);
        }

        _context.Videos.Remove(video);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IncrementViewCountAsync(int id)
    {
        var video = await _context.Videos.FindAsync(id);

        if (video == null)
        {
            return false;
        }

        video.Views++;
        _context.Update(video);
        await _context.SaveChangesAsync();

        return true;
    }
}