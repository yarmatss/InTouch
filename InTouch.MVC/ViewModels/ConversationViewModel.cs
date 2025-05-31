using InTouch.MVC.Models;

namespace InTouch.MVC.ViewModels;

public class ConversationViewModel
{
    public ApplicationUser OtherUser { get; set; }
    public List<Message> Messages { get; set; }
    public DateTime LastActive { get; set; }
}
