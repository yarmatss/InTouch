using InTouch.MVC.Models;
using InTouch.MVC.Services;
using InTouch.MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InTouch.MVC.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly IMessageService _messageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(
        IMessageService messageService,
        UserManager<ApplicationUser> userManager)
    {
        _messageService = messageService;
        _userManager = userManager;
    }

    // GET: /Messages
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var conversationPartners = await _messageService.GetConversationPartnersAsync(currentUser.Id);

        return View(conversationPartners);
    }

    // GET: /Messages/Conversation/userId
    public async Task<IActionResult> Conversation(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest();
        }

        var currentUser = await _userManager.GetUserAsync(HttpContext.User);
        var otherUser = await _userManager.FindByIdAsync(userId);

        if (otherUser == null)
        {
            return NotFound();
        }

        var messages = await _messageService.GetConversationAsync(currentUser.Id, userId);

        var viewModel = new ConversationViewModel
        {
            OtherUser = otherUser,
            Messages = messages,
            LastActive = await _messageService.GetLastActivityAsync(userId)
        };

        return View(viewModel);
    }
}
