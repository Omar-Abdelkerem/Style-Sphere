using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin;

// This page displays various reports and statistics for the admin
public class ReportsModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public ReportsModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public void OnGet()
    {
        // Step 1: Get all categories
        var categories = _db.Categories.OrderBy(c => c.Name).ToList();

        // Step 2: Count products in each category
        // Create a dictionary to store category name and product count
        var productCountByCategory = new Dictionary<string, int>();
        
        foreach (var category in categories)
        {
            // Count how many products belong to this category
            int count = _db.Products.Count(p => p.CategoryID == category.CategoryID);
            productCountByCategory[category.Name] = count;
        }

        // Step 3: Get products with low stock (stock <= 10)
        var lowStockProducts = _db.Products
            .Include(p => p.Category)
            .Where(p => p.Stock <= 10)
            .OrderBy(p => p.Stock)  // Order by stock (lowest first)
            .ToList();

        // Step 4: Calculate total revenue from completed orders
        // Sum all TotalAmount from orders where Status = "Completed"
        decimal totalRevenue = 0;
        var completedOrders = _db.Orders.Where(o => o.Status == "Completed").ToList();
        foreach (var order in completedOrders)
        {
            totalRevenue += order.TotalAmount;
        }

        // Step 5: Count orders by status
        // Create a dictionary to store status and count
        var ordersByStatus = new Dictionary<string, int>();
        
        // Get all possible order statuses
        var allOrders = _db.Orders.ToList();
        var statuses = new[] { "Processing", "Shipped", "Completed", "Cancelled" };
        
        foreach (var status in statuses)
        {
            // Count orders with this status
            int count = allOrders.Count(o => o.Status == status);
            ordersByStatus[status] = count;
        }

        // Step 6: Store all data in ViewData so the .cshtml file can use it
        ViewData["ProductCountByCategory"] = productCountByCategory;
        ViewData["LowStockProducts"] = lowStockProducts;
        ViewData["TotalRevenue"] = totalRevenue;
        ViewData["OrdersByStatus"] = ordersByStatus;
    }
}

