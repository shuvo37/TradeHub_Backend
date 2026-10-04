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

                    // Deleting a product keeps the post and just clears its ProductId
        modelBuilder.Entity<Post>()
            .HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

     // A like is identified by (post, user): the same user can't like a post twice
        modelBuilder.Entity<Like>()
            .HasKey(l => new { l.PostId, l.UserId });
    }
}