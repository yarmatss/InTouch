namespace InTouch.MVC.Models;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }

    public string SenderId { get; set; }
    public virtual ApplicationUser Sender { get; set; }

    public string ReceiverId { get; set; }
    public virtual ApplicationUser Receiver { get; set; }
}
