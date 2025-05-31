using InTouch.MVC.Models;
using InTouch.MVC.Services;
using InTouch.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InTouch.MVC.Controllers;

public class VideosController : Controller
{
    private readonly IVideoService _videoService;
    private readonly UserManager<ApplicationUser> _userManager;

    public VideosController(
        IVideoService videoService,
        UserManager<ApplicationUser> userManager)
    {
        _videoService = videoService;
        _userManager = userManager;
    }

    // GET: /Videos
    public async Task<IActionResult> Index(string sortBy = "date")
    {
        var videos = await _videoService.GetAllVideosAsync(sortBy);

        var viewModel = new VideosIndexViewModel
        {
            Videos = videos,
            SortBy = sortBy
        };

        return View(viewModel);
    }

    // GET: /Videos/User/userId
    public async Task<IActionResult> UserMusic(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var videos = await _videoService.GetUserVideosAsync(userId);

        var viewModel = new UserVideosViewModel
        {
            User = user,
            Videos = videos
        };

        return View(viewModel);
    }

    // GET: /Videos/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var video = await _videoService.GetVideoByIdAsync(id);

        if (video == null)
        {
            return NotFound();
        }

        // Increment view count if it's not the owner viewing
        if (User.Identity?.Name != video.User?.UserName)
        {
            await _videoService.IncrementViewCountAsync(id);
        }

        return View(video);
    }

    // GET: /Videos/Upload
    [Authorize]
    public IActionResult Upload()
    {
        return View();
    }

    // POST: /Videos/Upload
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload(UploadVideoViewModel model)
    {
        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);

            var video = await _videoService.UploadVideoAsync(
                currentUser.Id,
                model.Title,
                model.Description,
                model.VideoFile,
                model.ThumbnailFile);

            if (video != null)
            {
                return RedirectToAction(nameof(Details), new { id = video.Id });
            }

            ModelState.AddModelError("", "Failed to upload video. Please ensure the file is a valid video format.");
        }

        return View(model);
    }

    // POST: /Videos/Delete/5
    [HttpDelete]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);

        var result = await _videoService.DeleteVideoAsync(id, currentUser.Id);

        if (!result)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(User), new { userId = currentUser.Id });
    }
}