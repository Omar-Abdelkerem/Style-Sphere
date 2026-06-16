using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Users;

// This page allows admin to delete a user
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
        // Step 1: Check if user ID was provided
        if (id == null)
        {
            return NotFound();
        }

        // Step 2: Get user from database
        var user = _db.Users.Find(id.Value);
        if (user == null)
        {
            return NotFound();
        }

        // Step 3: Store user in ViewData so the .cshtml file can use it
        ViewData["User"] = user;

        return Page();
    }

    // This method runs when the delete button is clicked (POST request)
    // id parameter comes from the form
    public IActionResult OnPost(int? id)
    {
        // Step 1: Check if user ID was provided
        if (id == null)
        {
            return NotFound();
        }

        // Step 2: Get user from database
        var user = _db.Users.Find(id.Value);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("./Index");
        }

        // Step 3: Delete user from database
        _db.Users.Remove(user);
        _db.SaveChanges();

        // Step 4: Show success message and redirect to user list
        TempData["SuccessMessage"] = "User deleted successfully!";
        return RedirectToPage("./Index");
    }
}

