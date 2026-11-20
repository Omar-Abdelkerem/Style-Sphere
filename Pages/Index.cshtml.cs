using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AppDbContext db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public void OnGet()
    {
        try
        {
            // Bounded SQL query (SELECT TOP 12 / LIMIT 12) instead of loading entire table into memory
            var featuredProducts = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .OrderBy(p => p.ProductID)
                .Take(12)
                .ToList();

            ViewData["Products"] = featuredProducts;
        }
        catch (Exception ex)
        {
            ViewData["Products"] = new List<Product>();
            ViewData["ErrorMessage"] = "Unable to load catalog items at this time.";
            _logger.LogError(ex, "Database error in Index.OnGet");
        }
    }
}
