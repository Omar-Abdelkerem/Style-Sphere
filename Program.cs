using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using StyleSphere.Data;
using StyleSphere.Models;

var builder = WebApplication.CreateBuilder(args);

// Step 1: Add Razor Pages support
builder.Services.AddRazorPages();

// Step 2: Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Step 3: Add database connection
// Use LocalDB connection string with timeout
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=StyleSphere;Integrated Security=True;TrustServerCertificate=True;Connection Timeout=60;";

// Register the database context
// Enable retry on failure for connection issues
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    });
    options.EnableSensitiveDataLogging(false);
});

var app = builder.Build();

// Step 4: Create database and add initial data (if needed)
// This code runs when the application starts
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        // Wait a moment for LocalDB to be fully ready
        System.Threading.Thread.Sleep(2000);
        
        // Test connection first
        if (db.Database.CanConnect())
        {
            // Database exists and is accessible
            // Only create tables if they don't exist (don't recreate if they do)
            // Check if tables exist by trying to query Categories
            try
            {
                var test = db.Categories.Count();
                // If we get here, tables exist - don't recreate
            }
            catch
            {
                // Tables don't exist, create them
                db.Database.EnsureCreated();
            }

            // Check if we have any categories in the database
            // If not, add some default categories
            if (!db.Categories.Any())
            {
                db.Categories.AddRange(
                    new Category { Name = "Men", Description = "Clothing and accessories for men" },
                    new Category { Name = "Women", Description = "Clothing and accessories for women" },
                    new Category { Name = "Shoes", Description = "Footwear for all occasions" },
                    new Category { Name = "Accessories", Description = "Fashion accessories and add-ons" }
                );
                db.SaveChanges();
            }

            // Check if admin user exists
            // If not, create a default admin user
            if (!db.Users.Any(u => u.Email == "admin@stylesphere.com"))
            {
                db.Users.Add(new User
                {
                    Name = "Admin",
                    Email = "admin@stylesphere.com",
                    Password = "admin123",
                    Role = "Admin"
                });
                db.SaveChanges();
            }
        }
        else
        {
            Console.WriteLine("WARNING: Cannot connect to database. Please check your connection string and ensure LocalDB is running.");
        }
    }
    catch (Exception ex)
    {
        // If something goes wrong, log the error but don't crash the app
        Console.WriteLine("Database seeding error: " + ex.Message);
        Console.WriteLine("Stack trace: " + ex.StackTrace);
    }
}

// =====================
// 5?? Middleware Pipeline
// =====================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
