using InTouch.MVC.Data;
using InTouch.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace InTouch.MVC.Services;

public class PostService : IPostService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public PostService(ApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<List<Post>> GetFeedPostsAsync(string userId)
    {
        // Get IDs of friends
        var friendships = await _context.Friendships
            .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) &&
                        f.Status == FriendshipStatusEnum.Accepted)
            .ToListAsync();

        var friendIds = friendships
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToList();

        // Add current user ID to include their posts
        friendIds.Add(userId);

        // Get posts from current user and friends
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .Include(p => p.Likes)
            .Where(p => friendIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post> GetPostByIdAsync(int id)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .Include(p => p.Likes)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Post> CreatePostAsync(string userId, string content, IFormFile? media)
    {
        var post = new Post
        {
            Content = content,
            UserId = userId,
            CreatedAt = DateTime.Now
            // No need to set MediaUrl if it's nullable - it will default to null
        };

        if (media != null)
        {
            // Process and save media file
            string mediaUrl = await _fileStorage.SaveFile(media, "posts");
            post.MediaUrl = mediaUrl;

            // Determine media type
            string contentType = media.ContentType.ToLower();
            if (contentType.StartsWith("image/"))
            {
                post.MediaType = MediaType.Image;
            }
            else if (contentType.StartsWith("video/"))
            {
                post.MediaType = MediaType.Video;
            }
        }

        await _context.Posts.AddAsync(post);
        await _context.SaveChangesAsync();

        return post;
    }

    public async Task<Post> UpdatePostAsync(int id, string userId, string content)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null || post.UserId != userId)
        {
            return null;
        }

        post.Content = content;
        post.UpdatedAt = DateTime.Now;

        _context.Update(post);
        await _context.SaveChangesAsync();

        return post;
    }

    public async Task DeletePostAsync(int id, string userId)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null || post.UserId != userId)
        {
            return;
        }

        // Delete media file if exists
        if (!string.IsNullOrEmpty(post.MediaUrl))
        {
            _fileStorage.DeleteFile(post.MediaUrl);
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> LikePostAsync(int postId, string userId)
    {
        var post = await _context.Posts.FindAsync(postId);

        if (post == null)
        {
            return false;
        }

        // Check if user already liked this post
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike == null)
        {
            // Add new like
            _context.Likes.Add(new Like
            {
                PostId = postId,
                UserId = userId
            });

            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> UnlikePostAsync(int postId, string userId)
    {
        // Find existing like
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

        if (existingLike != null)
        {
            // Remove like
            _context.Likes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<Comment> AddCommentAsync(int postId, string userId, string content)
    {
        var post = await _context.Posts.FindAsync(postId);

        if (post == null || string.IsNullOrEmpty(content))
        {
            return null;
        }

        var comment = new Comment
        {
            Content = content,
            CreatedAt = DateTime.Now,
            PostId = postId,
            UserId = userId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return comment;
    }

    public async Task<List<Post>> GetUserPostsAsync(string userId)
    {
        return await _context.Posts
            .Include(p => p.User)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
            .Include(p => p.Likes)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}