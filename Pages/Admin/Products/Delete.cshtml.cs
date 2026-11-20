using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin.Products;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet(int? id)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (id == null)
        {
            return NotFound();
        }

        var product = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefault(p => p.ProductID == id.Value);

        if (product == null)
        {
            return NotFound();
        }

        ViewData["Product"] = product;
        return Page();
    }

    public IActionResult OnPost(int? id)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (id == null)
        {
            return NotFound();
        }

        var product = _db.Products.Find(id.Value);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("./Index");
        }

        if (_db.OrderItems.Any(oi => oi.ProductID == id.Value))
        {
            TempData["ErrorMessage"] = $"Cannot delete '{product.Name}' because it is referenced in past customer orders. Set its stock to 0 instead.";
            return RedirectToPage("./Index");
        }

        // Clean up any active shopping bag references before deleting
        var cartItems = _db.CartItems.Where(ci => ci.ProductID == id.Value).ToList();
        if (cartItems.Count > 0)
        {
            _db.CartItems.RemoveRange(cartItems);
        }

        _db.Products.Remove(product);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToPage("./Index");
    }
}
