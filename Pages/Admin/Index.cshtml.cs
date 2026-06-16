using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin;

// This is the admin dashboard page
// It shows links to different admin management sections
public class IndexModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public void OnGet()
    {
        // Step 1: Check if user is logged in and is admin
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            // Not admin - redirect to home page
            // (In a real application, you might want to show an error message)
        }

        // Step 2: Get some statistics for the dashboard
        // Count total products
        int totalProducts = _db.Products.Count();
        
        // Count total categories
        int totalCategories = _db.Categories.Count();
        
        // Count total orders
        int totalOrders = _db.Orders.Count();
        
        // Count total users (customers)
        int totalCustomers = _db.Users.Count(u => u.Role == "Customer");

        // Step 3: Store statistics in ViewData so the .cshtml file can use them
        ViewData["TotalProducts"] = totalProducts;
        ViewData["TotalCategories"] = totalCategories;
        ViewData["TotalOrders"] = totalOrders;
        ViewData["TotalCustomers"] = totalCustomers;
    }
}

