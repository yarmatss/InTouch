using InTouch.MVC.Models;
using InTouch.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InTouch.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IPostService _postService;
    private readonly IFriendService _friendService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public HomeController(
        IPostService postService,
        IFriendService friendService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _postService = postService;
        _friendService = friendService;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        // If user is authenticated, show posts feed
        if (User.Identity!.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            var posts = await _postService.GetFeedPostsAsync(currentUser!.Id);
            return View("Feed", posts);
        }

        // Otherwise show landing page
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Feed()
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var posts = await _postService.GetFeedPostsAsync(currentUser!.Id);
        return View(posts);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}