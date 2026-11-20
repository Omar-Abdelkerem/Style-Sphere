using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin.Categories;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
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

        var category = _db.Categories.Find(id.Value);
        if (category == null)
        {
            return NotFound();
        }

        ViewData["Category"] = category;
        return Page();
    }

    public IActionResult OnPost(int categoryId, string? name, string? description)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        var category = _db.Categories.Find(categoryId);
        if (category == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Category name is required.";
            ViewData["Category"] = category;
            return Page();
        }

        var trimmedName = name.Trim();
        var trimmedDesc = description?.Trim();

        if (trimmedName.Length > 100)
        {
            ViewData["ErrorMessage"] = "Category name must not exceed 100 characters.";
            ViewData["Category"] = category;
            return Page();
        }

        if (!string.IsNullOrEmpty(trimmedDesc) && trimmedDesc.Length > 500)
        {
            ViewData["ErrorMessage"] = "Category description must not exceed 500 characters.";
            ViewData["Category"] = category;
            return Page();
        }

        var existingCategory = _db.Categories
            .FirstOrDefault(c => c.Name.ToLower() == trimmedName.ToLower() && c.CategoryID != categoryId);
        if (existingCategory != null)
        {
            ViewData["ErrorMessage"] = "Category name already exists. Please choose a different name.";
            ViewData["Category"] = category;
            return Page();
        }

        category.Name = trimmedName;
        category.Description = trimmedDesc;
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Category updated successfully.";
        return RedirectToPage("./Index");
    }
}
