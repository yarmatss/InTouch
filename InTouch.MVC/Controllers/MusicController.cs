using InTouch.MVC.Models;
using InTouch.MVC.Services;
using InTouch.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InTouch.MVC.Controllers;

public class MusicController : Controller
{
    private readonly IMusicService _musicService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MusicController(
        IMusicService musicService,
        UserManager<ApplicationUser> userManager)
    {
        _musicService = musicService;
        _userManager = userManager;
    }

    // GET: /Music
    public async Task<IActionResult> Index(string sortBy = "date")
    {
        var music = await _musicService.GetAllMusicAsync(sortBy);

        var viewModel = new MusicIndexViewModel
        {
            Music = music,
            SortBy = sortBy
        };

        return View(viewModel);
    }

    // GET: /Music/User/userId
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

        var music = await _musicService.GetUserMusicAsync(userId);

        var viewModel = new UserMusicViewModel
        {
            User = user,
            Music = music
        };

        return View(viewModel);
    }

    // GET: /Music/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var music = await _musicService.GetMusicByIdAsync(id);

        if (music == null)
        {
            return NotFound();
        }

        return View(music);
    }

    // GET: /Music/Upload
    [Authorize]
    public IActionResult Upload()
    {
        return View();
    }

    // POST: /Music/Upload
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Upload(UploadMusicViewModel model)
    {
        if (ModelState.IsValid)
        {
            // Use HttpContext.User to be completely explicit
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);

            var music = await _musicService.UploadMusicAsync(
                currentUser.Id,
                model.Title,
                model.Description,
                model.Genre,
                model.MusicFile,
                model.CoverFile);

            if (music != null)
            {
                return RedirectToAction(nameof(Details), new { id = music.Id });
            }

            ModelState.AddModelError("", "Failed to upload audio. Please ensure the file is a valid audio format.");
        }

        return View(model);
    }

    // POST: /Music/Delete/5
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        // Use HttpContext.User to be completely explicit
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);

        var result = await _musicService.DeleteMusicAsync(id, currentUser.Id);

        if (!result)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(User), new { userId = currentUser.Id });
    }
}