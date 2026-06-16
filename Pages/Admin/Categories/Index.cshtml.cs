using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Categories;

// This page displays all categories for admin management
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
        // Step 1: Get all categories from database
        var categories = _db.Categories.OrderBy(c => c.Name).ToList();

        // Step 2: Count products in each category
        // Create a dictionary to store category ID and product count
        var productCounts = new Dictionary<int, int>();
        
        foreach (var category in categories)
        {
            // Count how many products belong to this category
            int count = _db.Products.Count(p => p.CategoryID == category.CategoryID);
            productCounts[category.CategoryID] = count;
        }

        // Step 3: Store data in ViewData so the .cshtml file can use it
        ViewData["Categories"] = categories;
        ViewData["ProductCounts"] = productCounts;
    }
}

