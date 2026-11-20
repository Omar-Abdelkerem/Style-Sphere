using Microsoft.EntityFrameworkCore;
using StyleSphere.Models;

namespace StyleSphere.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Shipper> Shippers { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(e => e.UserID);
            entity.Property(e => e.Email).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.SessionVersion).HasDefaultValue(1);
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.CategoryID);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(e => e.ProductID);
            entity.Property(e => e.ImageName).HasColumnName("Image");
            entity.HasIndex(e => e.CategoryID);
            entity.HasIndex(e => e.Stock);
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Cart entity
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Cart");
            entity.HasKey(e => e.CartID);
            entity.HasIndex(e => e.UserID);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CartItem entity
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItem");
            entity.HasKey(e => e.CartItemID);
            entity.HasIndex(e => new { e.CartID, e.ProductID }).IsUnique();
            entity.HasOne(e => e.Cart)
                  .WithMany(c => c.CartItems)
                  .HasForeignKey(e => e.CartID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Order entity
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Order");
            entity.HasKey(e => e.OrderID);
            entity.Property(e => e.OrderDate).HasColumnType("date");
            entity.HasIndex(e => e.UserID);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OrderDate);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure OrderItem entity
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");
            entity.HasKey(e => e.OrderItemID);
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(e => e.OrderID)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");
            entity.HasKey(e => e.PaymentID);
            entity.Property(e => e.PaymentDate).HasColumnType("date");
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Payments)
                  .HasForeignKey(e => e.OrderID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Shipper entity
        modelBuilder.Entity<Shipper>(entity =>
        {
            entity.ToTable("Shipper");
            entity.HasKey(e => e.ShipperID);
        });

        // Configure Shipment entity
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("Shipment");
            entity.HasKey(e => e.ShipmentID);
            entity.HasOne(e => e.Shipper)
                  .WithMany(s => s.Shipments)
                  .HasForeignKey(e => e.ShipperID)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.Order)
                  .WithMany(o => o.Shipments)
                  .HasForeignKey(e => e.OrderID)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Review entity
        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Review");
            entity.HasKey(e => e.ReviewID);
            entity.Property(e => e.ReviewDate).HasColumnType("date");
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductID)
                  .OnDelete(DeleteBehavior.Cascade);
            // Ensure one review per user per product
            entity.HasIndex(e => new { e.UserID, e.ProductID }).IsUnique();
        });

        // Configure PasswordResetToken entity
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetToken");
            entity.HasKey(e => e.TokenID);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserID);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserID)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
