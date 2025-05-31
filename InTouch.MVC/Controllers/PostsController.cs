using InTouch.MVC.Models;
using InTouch.MVC.Services;
using InTouch.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InTouch.MVC.Controllers;

[Authorize]
public class PostsController : Controller
{
    private readonly IPostService _postService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PostsController(
        IPostService postService,
        UserManager<ApplicationUser> userManager)
    {
        _postService = postService;
        _userManager = userManager;
    }

    // GET: /Posts
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var posts = await _postService.GetFeedPostsAsync(currentUser!.Id);
        return View(posts);
    }

    // GET: /Posts/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Posts/Create
    [HttpPost]
    public async Task<IActionResult> Create(CreatePostViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(this.User);
            await _postService.CreatePostAsync(user!.Id, model.Content!, model.Media!);
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // GET: /Posts/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        return View(post);
    }

    // GET: /Posts/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (post.UserId != currentUser!.Id)
        {
            return Forbid();
        }

        var model = new EditPostViewModel
        {
            Id = post.Id,
            Content = post.Content
        };

        return View(model);
    }

    // POST: /Posts/Edit/5
    [HttpPost]
    public async Task<IActionResult> Edit(int id, EditPostViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var post = await _postService.UpdatePostAsync(id, currentUser!.Id, model.Content!);

            if (post == null)
            {
                return NotFound();
            }

            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        return View(model);
    }

    // POST: /Posts/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _postService.DeletePostAsync(id, currentUser!.Id);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Posts/Like/5
    [HttpPost]
    public async Task<IActionResult> Like(int id)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _postService.LikePostAsync(id, currentUser!.Id);
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Posts/Unlike/5
    [HttpPost]
    public async Task<IActionResult> Unlike(int id)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _postService.UnlikePostAsync(id, currentUser!.Id);
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Posts/AddComment
    [HttpPost]
    public async Task<IActionResult> AddComment(int postId, string content)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _postService.AddCommentAsync(postId, currentUser!.Id, content);
        return RedirectToAction(nameof(Details), new { id = postId });
    }
}