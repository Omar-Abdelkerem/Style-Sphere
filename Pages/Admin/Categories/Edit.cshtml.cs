using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Categories;

// This page allows admin to edit an existing category
public class EditModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    // id parameter comes from the URL
    public IActionResult OnGet(int? id)
    {
        // Step 1: Check if category ID was provided
        if (id == null)
        {
            return NotFound();
        }

        // Step 2: Get category from database
        var category = _db.Categories.Find(id.Value);
        if (category == null)
        {
            return NotFound();
        }

        // Step 3: Store category in ViewData so the .cshtml file can use it
        ViewData["Category"] = category;

        return Page();
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(int categoryId, string name, string description)
    {
        // Step 1: Get category from database first
        var category = _db.Categories.Find(categoryId);
        if (category == null)
        {
            return NotFound();
        }

        // Step 2: Validate that category name is provided
        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Category name is required.";
            ViewData["Category"] = category;
            return Page();
        }
        if (category == null)
        {
            return NotFound();
        }

        // Step 3: Check if category name already exists (excluding current category)
        var existingCategory = _db.Categories.FirstOrDefault(c => c.Name == name && c.CategoryID != categoryId);
        if (existingCategory != null)
        {
            ViewData["ErrorMessage"] = "Category name already exists. Please choose a different name.";
            ViewData["Category"] = category;
            return Page();
        }

        // Step 4: Update category information
        category.Name = name;
        category.Description = description;

        // Step 5: Save changes to database
        _db.SaveChanges();

        // Step 6: Show success message and redirect to category list
        TempData["SuccessMessage"] = "Category updated successfully!";
        return RedirectToPage("./Index");
    }
}

