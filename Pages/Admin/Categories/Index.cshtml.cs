using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin.Categories;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        var userRole = HttpContext.Session.GetString("UserRole");
        if (userRole != "Admin")
        {
            return RedirectToPage("/login");
        }

        var categories = _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToList();

        // Single batched SQL GroupBy query instead of N+1 loop per category
        var productCounts = _db.Products
            .AsNoTracking()
            .GroupBy(p => p.CategoryID)
            .Select(g => new { CategoryID = g.Key, Count = g.Count() })
            .ToDictionary(x => x.CategoryID, x => x.Count);

        ViewData["Categories"] = categories;
        ViewData["ProductCounts"] = productCounts;
        return Page();
    }
}
