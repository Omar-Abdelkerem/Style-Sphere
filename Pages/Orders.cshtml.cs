using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages;

// This page displays all orders for the logged-in user
public class OrdersModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public OrdersModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public IActionResult OnGet()
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            // User not logged in - redirect to login page
            return RedirectToPage("/login");
        }

        // Step 2: Get all orders for this user from database
        // Include OrderItems and Products so we can display order details
        var orders = _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.UserID == userId)
            .OrderByDescending(o => o.OrderDate)  // Most recent orders first
            .ToList();

        // Step 3: Store orders in ViewData so the .cshtml file can use them
        ViewData["Orders"] = orders;

        return Page();
    }
}

