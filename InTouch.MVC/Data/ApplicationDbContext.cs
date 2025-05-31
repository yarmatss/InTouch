using InTouch.MVC.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InTouch.MVC.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Music> Musics { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure nullable fields for ApplicationUser
        builder.Entity<ApplicationUser>()
            .Property(u => u.Bio)
            .IsRequired(false);

        builder.Entity<ApplicationUser>()
            .Property(u => u.ProfilePicture)
            .IsRequired(false);

        // Configure Post entity
        builder.Entity<Post>()
            .Property(p => p.MediaUrl)
            .IsRequired(false);

        builder.Entity<Post>()
            .HasOne(p => p.User)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Comment entity - break the cascade delete chain
        builder.Entity<Comment>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths

        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Like entity - break the cascade delete chain
        builder.Entity<Like>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths

        builder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Friendship entity
        builder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany(u => u.SentFriendRequests)
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Friendship>()
            .HasOne(f => f.Addressee)
            .WithMany(u => u.ReceivedFriendRequests)
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Message entity
        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Video entity
        builder.Entity<Video>()
            .HasOne(v => v.User)
            .WithMany(u => u.Videos)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Music entity
        builder.Entity<Music>()
            .HasOne(m => m.User)
            .WithMany(u => u.Musics)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure indices for better query performance
        builder.Entity<Post>()
            .HasIndex(p => p.UserId);

        builder.Entity<Post>()
            .HasIndex(p => p.CreatedAt);

        builder.Entity<Like>()
            .HasIndex(l => new { l.PostId, l.UserId })
            .IsUnique(); // Prevent duplicate likes

        builder.Entity<Friendship>()
            .HasIndex(f => new { f.RequesterId, f.AddresseeId })
            .IsUnique(); // Prevent duplicate friendship requests
    }
}
