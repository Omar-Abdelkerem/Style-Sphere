using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages;

public class OrdersModel : PageModel
{
    private readonly AppDbContext _db;

    public OrdersModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        var orders = _db.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.UserID == userId)
            .OrderByDescending(o => o.OrderDate)
            .ThenByDescending(o => o.OrderID)
            .Take(100)
            .ToList();

        ViewData["Orders"] = orders;
        return Page();
    }
}
