using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Categories;

// This page allows admin to delete a category
public class DeleteModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public DeleteModel(AppDbContext db)
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
        // Include Products to show how many products will be affected
        var category = _db.Categories
            .Include(c => c.Products)
            .FirstOrDefault(c => c.CategoryID == id.Value);

        if (category == null)
        {
            return NotFound();
        }

        // Step 3: Store category in ViewData so the .cshtml file can use it
        ViewData["Category"] = category;

        return Page();
    }

    // This method runs when the delete button is clicked (POST request)
    // id parameter comes from the form
    public IActionResult OnPost(int? id)
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
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToPage("./Index");
        }

        // Step 3: Delete category from database
        _db.Categories.Remove(category);
        _db.SaveChanges();

        // Step 4: Show success message and redirect to category list
        TempData["SuccessMessage"] = "Category deleted successfully!";
        return RedirectToPage("./Index");
    }
}

