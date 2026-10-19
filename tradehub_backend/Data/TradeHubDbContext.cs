using Microsoft.EntityFrameworkCore;
using TradeHub.Models;

namespace TradeHub.Data;

public class TradeHubDbContext : DbContext
{
    public TradeHubDbContext(DbContextOptions<TradeHubDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SearchHistory> SearchHistories => Set<SearchHistory>();
    public DbSet<UserContactEmail> ContactEmails => Set<UserContactEmail>();
    public DbSet<EmailCode> EmailCodes => Set<EmailCode>();
    public DbSet<EmailCodeSend> EmailCodeSends => Set<EmailCodeSend>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RefreshToken>().HasIndex(t => t.TokenHash).IsUnique();

        // One user cannot have two categories with the same name
        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.UserId, c.Name })
            .IsUnique();

        // One category cannot have two products with the same name
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.CategoryId, p.Name })
            .IsUnique();


        // Order has two links to User: the buyer and the seller
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany()
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Seller)
            .WithMany()
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deleting a product keeps the order (it has its own copy of the name and price) and just clears its ProductId
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Product)
            .WithMany()
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

                    // Deleting a product keeps the post and just clears its ProductId
        modelBuilder.Entity<Post>()
            .HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // The news feed and a profile both read "posts of these users, newest first"
        modelBuilder.Entity<Post>()
            .HasIndex(p => new { p.UserId, p.CreatedAt });

     // A like is identified by (post, user): the same user can't like a post twice
        modelBuilder.Entity<Like>()
            .HasKey(l => new { l.PostId, l.UserId });

        // A friendship links two users: the one who sent the request and the one who received it
        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Requester)
            .WithMany()
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Friendship>()
            .HasOne(f => f.Addressee)
            .WithMany()
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Cascade);

        // The same person can't send the same person two requests
        modelBuilder.Entity<Friendship>()
            .HasIndex(f => new { f.RequesterId, f.AddresseeId })
            .IsUnique();

        // Finding the requests I received
        modelBuilder.Entity<Friendship>()
            .HasIndex(f => f.AddresseeId);

        // A notification belongs to the person who sees it (recipient) and names the person who caused it (actor).
        // If either user is deleted, the notification goes with them.
        modelBuilder.Entity<Notification>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(n => n.RecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Loading my list (newest first) and counting my unread ones
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.RecipientId, n.CreatedAt });

        // Removing the "sent you a request" line when that request is answered
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.FriendshipId);

        // Removing the "commented on your post" line when that comment is deleted
        modelBuilder.Entity<Notification>()
            .HasIndex(n => n.CommentId);

        // A search text is identified by (user, text): the same user searching it again does not add a row
        modelBuilder.Entity<SearchHistory>()
            .HasKey(s => new { s.UserId, s.Term });

        modelBuilder.Entity<SearchHistory>()
            .Property(s => s.Term)
            .HasMaxLength(60);

        // If the user is deleted, their searches go with them
        modelBuilder.Entity<SearchHistory>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // "Texts that start with 'tri'" for all users. text_pattern_ops makes a normal index usable for LIKE 'tri%'
        // whatever the database collation is: this index does the job of the trie.
        modelBuilder.Entity<SearchHistory>()
            .HasIndex(s => s.Term)
            .HasOperators("text_pattern_ops");

        // One contact email per user (the key is the user id). Many users may share the same email, so no unique index on it.
        modelBuilder.Entity<UserContactEmail>()
            .HasKey(c => c.UserId);

        modelBuilder.Entity<UserContactEmail>()
            .Property(c => c.Email)
            .HasMaxLength(254);

        // If the user is deleted, their contact email goes with them
        modelBuilder.Entity<UserContactEmail>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The active confirmation code: one per user and purpose (a new code replaces the old row).
        // The purpose is stored as an integer, like the other enums.
        modelBuilder.Entity<EmailCode>()
            .HasKey(c => new { c.UserId, c.Purpose });

        modelBuilder.Entity<EmailCode>()
            .Property(c => c.Email)
            .HasMaxLength(254);

        modelBuilder.Entity<EmailCode>()
            .Property(c => c.CodeHash)
            .HasMaxLength(100); // a bcrypt hash is 60 characters

        modelBuilder.Entity<EmailCode>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The send log, only for the limits: the wait between codes, and the codes per hour for an account / an address
        modelBuilder.Entity<EmailCodeSend>()
            .Property(s => s.Email)
            .HasMaxLength(254);

        modelBuilder.Entity<EmailCodeSend>()
            .HasIndex(s => new { s.UserId, s.Purpose, s.SentAt });

        modelBuilder.Entity<EmailCodeSend>()
            .HasIndex(s => new { s.Email, s.Purpose, s.SentAt });

        modelBuilder.Entity<EmailCodeSend>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}