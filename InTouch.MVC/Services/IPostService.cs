using InTouch.MVC.Models;

namespace InTouch.MVC.Services;

public interface IPostService
{
    Task<List<Post>> GetFeedPostsAsync(string userId);
    Task<Post> GetPostByIdAsync(int id);
    Task<Post> CreatePostAsync(string userId, string content, IFormFile media);
    Task<Post> UpdatePostAsync(int id, string userId, string content);
    Task DeletePostAsync(int id, string userId);
    Task<bool> LikePostAsync(int postId, string userId);
    Task<bool> UnlikePostAsync(int postId, string userId);
    Task<Comment> AddCommentAsync(int postId, string userId, string content);
    Task<List<Post>> GetUserPostsAsync(string userId);
}