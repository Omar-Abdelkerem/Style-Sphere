using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Categories;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }
        return Page();
    }

    public IActionResult OnPost(string? name, string? description)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Category name is required.";
            return Page();
        }

        var trimmedName = name.Trim();
        var trimmedDesc = description?.Trim();

        if (trimmedName.Length > 100)
        {
            ViewData["ErrorMessage"] = "Category name must not exceed 100 characters.";
            return Page();
        }

        if (!string.IsNullOrEmpty(trimmedDesc) && trimmedDesc.Length > 500)
        {
            ViewData["ErrorMessage"] = "Category description must not exceed 500 characters.";
            return Page();
        }

        var existingCategory = _db.Categories.FirstOrDefault(c => c.Name.ToLower() == trimmedName.ToLower());
        if (existingCategory != null)
        {
            ViewData["ErrorMessage"] = "Category name already exists. Please choose a different name.";
            return Page();
        }

        var newCategory = new Category
        {
            Name = trimmedName,
            Description = trimmedDesc
        };

        _db.Categories.Add(newCategory);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Category created successfully.";
        return RedirectToPage("./Index");
    }
}
