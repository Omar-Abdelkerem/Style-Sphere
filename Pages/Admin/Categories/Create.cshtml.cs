using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Categories;

// This page allows admin to create a new category
public class CreateModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    // Just show the form
    public void OnGet()
    {
        // No special logic needed, just show the page
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(string name, string description)
    {
        // Step 1: Validate that category name is provided
        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Category name is required.";
            return Page();
        }

        // Step 2: Check if category name already exists
        // Category names must be unique
        var existingCategory = _db.Categories.FirstOrDefault(c => c.Name == name);
        if (existingCategory != null)
        {
            ViewData["ErrorMessage"] = "Category name already exists. Please choose a different name.";
            return Page();
        }

        // Step 3: Create new category
        var newCategory = new Category
        {
            Name = name,
            Description = description  // Description is optional, so it can be empty
        };

        // Step 4: Add category to database and save
        _db.Categories.Add(newCategory);
        _db.SaveChanges();

        // Step 5: Show success message and redirect to category list
        TempData["SuccessMessage"] = "Category created successfully!";
        return RedirectToPage("./Index");
    }
}

