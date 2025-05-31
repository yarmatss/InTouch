using InTouch.MVC.Models;
using InTouch.MVC.Services;
using InTouch.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InTouch.MVC.Controllers;

[Authorize]
public class FriendsController : Controller
{
    private readonly IFriendService _friendService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FriendsController(
        IFriendService friendService,
        UserManager<ApplicationUser> userManager)
    {
        _friendService = friendService;
        _userManager = userManager;
    }

    // GET: /Friends
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var friends = await _friendService.GetFriendsAsync(currentUser!.Id);
        return View(friends);
    }

    // GET: /Friends/Requests
    public async Task<IActionResult> Requests()
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var requests = await _friendService.GetFriendRequestsAsync(currentUser!.Id);
        return View(requests);
    }

    // GET: /Friends/Find
    public async Task<IActionResult> Find(string searchTerm)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);

        var viewModel = new FindFriendsViewModel
        {
            SearchTerm = searchTerm
        };

        if (!string.IsNullOrEmpty(searchTerm))
        {
            viewModel.Results = await _friendService.SearchUsersAsync(currentUser!.Id, searchTerm);
        }

        return View(viewModel);
    }

    // POST: /Friends/SendRequest
    [HttpPost]
    public async Task<IActionResult> SendRequest(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _friendService.SendFriendRequestAsync(currentUser!.Id, userId);
        return RedirectToAction(nameof(Find));
    }

    // POST: /Friends/AcceptRequest
    [HttpPost]
    public async Task<IActionResult> AcceptRequest(int friendshipId)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _friendService.AcceptFriendRequestAsync(friendshipId, currentUser!.Id);
        return RedirectToAction(nameof(Requests));
    }

    // POST: /Friends/RejectRequest
    [HttpPost]
    public async Task<IActionResult> RejectRequest(int friendshipId)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _friendService.RejectFriendRequestAsync(friendshipId, currentUser!.Id);
        return RedirectToAction(nameof(Requests));
    }

    // POST: /Friends/Remove
    [HttpPost]
    public async Task<IActionResult> Remove(string userId)
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        await _friendService.RemoveFriendAsync(currentUser!.Id, userId);
        return RedirectToAction(nameof(Index));
    }
}