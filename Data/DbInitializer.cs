using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Models;
using StyleSphere.Services;

namespace StyleSphere.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Ensure database tables exist
        context.Database.EnsureCreated();

        // Ensure schema migrations & indexes for existing databases
        ApplyNonDestructiveSchemaUpdates(context);

        // 1. Seed Categories if empty
        if (!context.Categories.Any())
        {
            var categories = new Category[]
            {
                new Category { Name = "Men", Description = "Clothing and accessories for men" },
                new Category { Name = "Women", Description = "Clothing and accessories for women" },
                new Category { Name = "Shoes", Description = "Footwear for all occasions" },
                new Category { Name = "Accessories", Description = "Fashion accessories and add-ons" }
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();
        }

        var menCat = context.Categories.FirstOrDefault(c => c.Name == "Men");
        var womenCat = context.Categories.FirstOrDefault(c => c.Name == "Women");
        var shoesCat = context.Categories.FirstOrDefault(c => c.Name == "Shoes");
        var accCat = context.Categories.FirstOrDefault(c => c.Name == "Accessories");

        // 2. Seed Default Users if empty (using PBKDF2-HMAC-SHA512 password hashes)
        var initialAdminPassword = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD") ?? "admin123";

        if (!context.Users.Any(u => u.Email == "admin@stylesphere.com"))
        {
            context.Users.Add(new User
            {
                Name = "Admin",
                Email = "admin@stylesphere.com",
                Phone = "1234567890",
                Password = PasswordHasher.HashPassword(initialAdminPassword),
                Role = "Admin",
                SessionVersion = 1
            });
        }

        if (!context.Users.Any(u => u.Email == "o@gmail.com"))
        {
            context.Users.Add(new User
            {
                Name = "Omar",
                Email = "o@gmail.com",
                Phone = "01000000000",
                Password = PasswordHasher.HashPassword("123456"),
                Role = "Customer",
                SessionVersion = 1
            });
        }
        context.SaveChanges();

        // Automatically upgrade any legacy plaintext passwords in existing databases to PBKDF2 hashes
        var allUsers = context.Users.ToList();
        bool upgradedHashes = false;
        foreach (var user in allUsers)
        {
            if (!PasswordHasher.IsHashed(user.Password))
            {
                user.Password = PasswordHasher.HashPassword(user.Password);
                upgradedHashes = true;
            }
            if (user.SessionVersion <= 0)
            {
                user.SessionVersion = 1;
                upgradedHashes = true;
            }
        }
        if (upgradedHashes)
        {
            context.SaveChanges();
        }

        // 3. Seed Products if empty
        if (!context.Products.Any())
        {
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Classic Oxford Cotton Shirt",
                    Description = "Premium breathable 100% cotton button-down shirt designed for effortless business-casual elegance.",
                    Price = 49.99m,
                    Stock = 35,
                    ImageName = "shirt-oxford.jpg",
                    CategoryID = menCat != null ? menCat.CategoryID : 1
                },
                new Product
                {
                    Name = "Slim Fit Tailored Wool Blazer",
                    Description = "Modern single-breasted blazer woven from fine Italian wool blend with silk lining.",
                    Price = 129.99m,
                    Stock = 18,
                    ImageName = "blazer-wool.jpg",
                    CategoryID = menCat != null ? menCat.CategoryID : 1
                },
                new Product
                {
                    Name = "Vintage Wash Denim Jacket",
                    Description = "Classic rugged trucker jacket with dual chest flap pockets and distressed copper hardware.",
                    Price = 79.99m,
                    Stock = 28,
                    ImageName = "jacket-denim.jpg",
                    CategoryID = menCat != null ? menCat.CategoryID : 1
                },
                new Product
                {
                    Name = "Floral Breeze Summer Midi Dress",
                    Description = "Feminine A-line midi dress featuring delicate floral motifs, sweetheart neckline, and sash belt.",
                    Price = 59.99m,
                    Stock = 24,
                    ImageName = "dress-midi.jpg",
                    CategoryID = womenCat != null ? womenCat.CategoryID : 2
                },
                new Product
                {
                    Name = "Pure Cashmere Turtleneck Sweater",
                    Description = "Ultra-soft, lightweight Mongolian cashmere knit engineered for warmth and timeless sophistication.",
                    Price = 89.99m,
                    Stock = 20,
                    ImageName = "sweater-cashmere.jpg",
                    CategoryID = womenCat != null ? womenCat.CategoryID : 2
                },
                new Product
                {
                    Name = "High-Waisted Tailored Trousers",
                    Description = "Crisp front-pleated wide-leg trousers with an adjustable belt for contemporary chic styling.",
                    Price = 64.99m,
                    Stock = 22,
                    ImageName = "trousers-tailored.jpg",
                    CategoryID = womenCat != null ? womenCat.CategoryID : 2
                },
                new Product
                {
                    Name = "Minimalist Low-Top Leather Sneakers",
                    Description = "Handcrafted smooth calfskin sneakers with cushioned memory foam insole and cupsole.",
                    Price = 95.00m,
                    Stock = 40,
                    ImageName = "sneakers-minimal.jpg",
                    CategoryID = shoesCat != null ? shoesCat.CategoryID : 3
                },
                new Product
                {
                    Name = "Chelsea Burnished Leather Boots",
                    Description = "Iconic slip-on ankle boots with elasticated side gussets and robust stacked leather soles.",
                    Price = 139.99m,
                    Stock = 15,
                    ImageName = "boots-chelsea.jpg",
                    CategoryID = shoesCat != null ? shoesCat.CategoryID : 3
                },
                new Product
                {
                    Name = "Speed Pro Athletic Trainers",
                    Description = "High-performance shock-absorbing running sneakers engineered with responsive energy return.",
                    Price = 84.99m,
                    Stock = 30,
                    ImageName = "trainers-classic.jpg",
                    CategoryID = shoesCat != null ? shoesCat.CategoryID : 3
                },
                new Product
                {
                    Name = "Polarized Aviator Sunglasses",
                    Description = "Precision UV400 polarized shades with lightweight metal frames and gradient tint lenses.",
                    Price = 39.99m,
                    Stock = 50,
                    ImageName = "sunglasses-polar.jpg",
                    CategoryID = accCat != null ? accCat.CategoryID : 4
                },
                new Product
                {
                    Name = "Heritage Leather Crossbody Bag",
                    Description = "Full-grain artisan leather satchel featuring brushed brass buckle and multiple compartments.",
                    Price = 119.99m,
                    Stock = 14,
                    ImageName = "bag-leather.jpg",
                    CategoryID = accCat != null ? accCat.CategoryID : 4
                },
                new Product
                {
                    Name = "Chronograph Luxury Timepiece",
                    Description = "Refined stainless steel watch with tachymeter bezel, sapphire crystal glass, and leather strap.",
                    Price = 189.99m,
                    Stock = 12,
                    ImageName = "watch-chrono.jpg",
                    CategoryID = accCat != null ? accCat.CategoryID : 4
                }
            };
            context.Products.AddRange(products);
            context.SaveChanges();
        }

        // 4. Seed Initial Verified Reviews if empty
        if (!context.Reviews.Any())
        {
            var customer = context.Users.FirstOrDefault(u => u.Email == "o@gmail.com")
                        ?? context.Users.FirstOrDefault(u => u.Role == "Customer");

            if (customer != null)
            {
                var oxfordShirt = context.Products.FirstOrDefault(p => p.Name.Contains("Oxford"));
                var blazer = context.Products.FirstOrDefault(p => p.Name.Contains("Blazer"));
                var sneakers = context.Products.FirstOrDefault(p => p.Name.Contains("Sneakers"));
                var watch = context.Products.FirstOrDefault(p => p.Name.Contains("Timepiece"));
                var bag = context.Products.FirstOrDefault(p => p.Name.Contains("Bag"));

                var reviews = new List<Review>();
                if (oxfordShirt != null)
                {
                    reviews.Add(new Review
                    {
                        ReviewDate = DateTime.Parse("2026-10-15"),
                        Comment = "Exceptional cotton quality and fits true to size. Perfect for office and dinner dates!",
                        Rating = 5,
                        UserID = customer.UserID,
                        ProductID = oxfordShirt.ProductID
                    });
                }
                if (blazer != null)
                {
                    reviews.Add(new Review
                    {
                        ReviewDate = DateTime.Parse("2026-10-18"),
                        Comment = "Sublime stitching and great drape. Highly recommended.",
                        Rating = 5,
                        UserID = customer.UserID,
                        ProductID = blazer.ProductID
                    });
                }
                if (sneakers != null)
                {
                    reviews.Add(new Review
                    {
                        ReviewDate = DateTime.Parse("2026-10-22"),
                        Comment = "Incredibly soft and comfortable sneakers right out of the box!",
                        Rating = 5,
                        UserID = customer.UserID,
                        ProductID = sneakers.ProductID
                    });
                }
                if (watch != null)
                {
                    reviews.Add(new Review
                    {
                        ReviewDate = DateTime.Parse("2026-10-25"),
                        Comment = "The watch looks even better in person than in photos. Heavy and premium.",
                        Rating = 5,
                        UserID = customer.UserID,
                        ProductID = watch.ProductID
                    });
                }
                if (bag != null)
                {
                    reviews.Add(new Review
                    {
                        ReviewDate = DateTime.Parse("2026-10-28"),
                        Comment = "Beautiful bag, high quality leather and sturdy hardware.",
                        Rating = 5,
                        UserID = customer.UserID,
                        ProductID = bag.ProductID
                    });
                }

                if (reviews.Any())
                {
                    context.Reviews.AddRange(reviews);
                    context.SaveChanges();
                }
            }
        }
    }

    private static void ApplyNonDestructiveSchemaUpdates(AppDbContext context)
    {
        if (!context.Database.IsSqlite())
        {
            return;
        }

        try
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""PasswordResetToken"" (
                    ""TokenID"" INTEGER NOT NULL CONSTRAINT ""PK_PasswordResetToken"" PRIMARY KEY AUTOINCREMENT,
                    ""UserID"" INTEGER NOT NULL,
                    ""TokenHash"" TEXT NOT NULL,
                    ""ExpiresAt"" TEXT NOT NULL,
                    ""IsUsed"" INTEGER NOT NULL DEFAULT 0,
                    ""CreatedAt"" TEXT NOT NULL,
                    CONSTRAINT ""FK_PasswordResetToken_User_UserID"" FOREIGN KEY (""UserID"") REFERENCES ""User"" (""UserID"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_PasswordResetToken_TokenHash"" ON ""PasswordResetToken"" (""TokenHash"");
                CREATE INDEX IF NOT EXISTS ""IX_PasswordResetToken_UserID"" ON ""PasswordResetToken"" (""UserID"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Category_Name"" ON ""Category"" (""Name"");
                CREATE INDEX IF NOT EXISTS ""IX_Product_Stock"" ON ""Product"" (""Stock"");
                CREATE INDEX IF NOT EXISTS ""IX_Order_Status"" ON ""Order"" (""Status"");
                CREATE INDEX IF NOT EXISTS ""IX_Order_OrderDate"" ON ""Order"" (""OrderDate"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_CartItem_CartID_ProductID"" ON ""CartItem"" (""CartID"", ""ProductID"");
            ");
        }
        catch
        {
            // Ignore if already created
        }

        try
        {
            context.Database.ExecuteSqlRaw(@"ALTER TABLE ""User"" ADD COLUMN ""SessionVersion"" INTEGER NOT NULL DEFAULT 1;");
        }
        catch
        {
            // Column already exists
        }
    }
}
