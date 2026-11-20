using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin;

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

        ViewData["TotalProducts"] = _db.Products.Count();
        ViewData["TotalCategories"] = _db.Categories.Count();
        ViewData["TotalOrders"] = _db.Orders.Count();
        ViewData["TotalCustomers"] = _db.Users.Count(u => u.Role == "Customer");

        return Page();
    }
}
