using InTouch.MVC.Data;
using InTouch.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace InTouch.MVC.Services;

public class MusicService : IMusicService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public MusicService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<List<Music>> GetAllMusicAsync(string sortBy = "date")
    {
        IQueryable<Music> query = _context.Musics.Include(m => m.User);

        if (sortBy == "genre")
        {
            return await query.OrderBy(m => m.Genre).ToListAsync();
        }
        else if (sortBy == "artist")
        {
            return await query
                .OrderBy(m => !string.IsNullOrEmpty(m.Artist)) // Non-null artists first
                .ThenBy(m => m.Artist) // Then sort by artist name
                .ThenByDescending(m => m.UploadDate)
                .ToListAsync();
        }
        else if (sortBy == "user")
        {
            return await query
                .OrderBy(m => m.User.FirstName)
                .ThenBy(m => m.User.LastName)
                .ThenByDescending(m => m.UploadDate)
                .ToListAsync();
        }
        else // Default: date
        {
            return await query.OrderByDescending(m => m.UploadDate).ToListAsync();
        }
    }

    public async Task<List<Music>> GetUserMusicAsync(string userId)
    {
        return await _context.Musics
            .Include(m => m.User)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.UploadDate)
            .ToListAsync();
    }

    public async Task<Music> GetMusicByIdAsync(int id)
    {
        return await _context.Musics
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Music> UploadMusicAsync(string userId, string title, string artist, string description, 
                                                string genre, IFormFile musicFile, IFormFile coverFile = null)
    {
        if (musicFile == null || musicFile.Length == 0)
        {
            return null;
        }

        // Validate file is audio
        if (!musicFile.ContentType.StartsWith("audio/"))
        {
            return null;
        }

        // Save music file
        string musicUrl = await _fileStorage.SaveFile(musicFile, "music");

        // Save cover if provided
        string coverUrl = null;
        if (coverFile != null && coverFile.Length > 0)
        {
            coverUrl = await _fileStorage.SaveFile(coverFile, "covers");
        }
        else
        {
            // Default cover
            coverUrl = "/images/default-music-cover.jpg";
        }

        var music = new Music
        {
            Title = title,
            Artist = artist,
            Description = description,
            Genre = genre,
            MusicUrl = musicUrl,
            CoverUrl = coverUrl,
            UploadDate = DateTime.UtcNow,
            UserId = userId
        };

        _context.Musics.Add(music);
        await _context.SaveChangesAsync();

        return music;
    }

    public async Task<bool> DeleteMusicAsync(int id, string userId)
    {
        var music = await _context.Musics.FindAsync(id);

        if (music == null || music.UserId != userId)
        {
            return false;
        }

        // Delete files
        if (!string.IsNullOrEmpty(music.MusicUrl))
        {
            _fileStorage.DeleteFile(music.MusicUrl);
        }

        if (!string.IsNullOrEmpty(music.CoverUrl) &&
            !music.CoverUrl.EndsWith("default-music-cover.jpg"))
        {
            _fileStorage.DeleteFile(music.CoverUrl);
        }

        _context.Musics.Remove(music);
        await _context.SaveChangesAsync();

        return true;
    }
}
