﻿namespace InTouch.MVC.Models;

public class Like
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public virtual Post Post { get; set; }

    public string UserId { get; set; }
    public virtual ApplicationUser User { get; set; }
}