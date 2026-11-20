using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin;

public class ReportsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public ReportsModel(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public IActionResult OnGet()
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/login");
        }

        // 1. Batched query for categories + product counts (eliminates N+1 loop)
        var categories = _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToList();

        var countsByCatId = _db.Products
            .AsNoTracking()
            .GroupBy(p => p.CategoryID)
            .Select(g => new { CategoryID = g.Key, Count = g.Count() })
            .ToDictionary(x => x.CategoryID, x => x.Count);

        var productCountByCategory = new Dictionary<string, int>();
        foreach (var category in categories)
        {
            productCountByCategory[category.Name] = countsByCatId.TryGetValue(category.CategoryID, out int count) ? count : 0;
        }

        // 2. Low stock products (using configured threshold)
        int lowStockThreshold = _config.GetValue<int>("StoreSettings:LowStockThreshold", 10);
        var lowStockProducts = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Stock <= lowStockThreshold)
            .OrderBy(p => p.Stock)
            .Take(100)
            .ToList();

        // 3. Single grouped SQL query for order counts and revenue by status
        var orderStats = _db.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => (double)o.TotalAmount)
            })
            .ToList();

        decimal totalRevenue = (decimal)orderStats
            .Where(s => s.Status == "Completed" || s.Status == "Processing")
            .Sum(s => s.Revenue);

        var ordersByStatus = new Dictionary<string, int>();
        var statuses = new[] { "Processing", "Shipped", "Completed", "Cancelled" };
        foreach (var status in statuses)
        {
            var match = orderStats.FirstOrDefault(s => s.Status == status);
            ordersByStatus[status] = match?.Count ?? 0;
        }

        ViewData["ProductCountByCategory"] = productCountByCategory;
        ViewData["LowStockProducts"] = lowStockProducts;
        ViewData["TotalRevenue"] = totalRevenue;
        ViewData["OrdersByStatus"] = ordersByStatus;

        return Page();
    }
}
