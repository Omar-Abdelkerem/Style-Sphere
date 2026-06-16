using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Products;

// This page displays all products for admin management
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
        // Step 1: Get all products from database
        // Include Category information so we can display category names
        var products = _db.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .ToList();

        // Step 2: Store products in ViewData so the .cshtml file can use them
        ViewData["Products"] = products;
    }
}

