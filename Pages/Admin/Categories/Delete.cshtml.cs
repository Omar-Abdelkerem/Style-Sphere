using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin.Categories;

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

        var category = _db.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefault(c => c.CategoryID == id.Value);

        if (category == null)
        {
            return NotFound();
        }

        ViewData["Category"] = category;
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

        var category = _db.Categories.Find(id.Value);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToPage("./Index");
        }

        if (_db.Products.Any(p => p.CategoryID == id.Value))
        {
            TempData["ErrorMessage"] = $"Cannot delete '{category.Name}' while products are still assigned to it. Reassign or delete its products first.";
            return RedirectToPage("./Index");
        }

        _db.Categories.Remove(category);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Category deleted successfully.";
        return RedirectToPage("./Index");
    }
}
