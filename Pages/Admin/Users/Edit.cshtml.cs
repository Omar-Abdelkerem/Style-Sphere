using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Users;

// This page allows admin to edit an existing user
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

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(int userId, string name, string email, string phone, string password, string role)
    {
        // Step 1: Validate that required fields are provided
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            ViewData["ErrorMessage"] = "Please fill in all required fields (Name, Email).";
            // Reload user data
            var user = _db.Users.Find(userId);
            ViewData["User"] = user;
            return Page();
        }

        // Step 2: Validate role (must be Admin or Customer)
        if (string.IsNullOrWhiteSpace(role) || (role != "Admin" && role != "Customer"))
        {
            ViewData["ErrorMessage"] = "Role must be either 'Admin' or 'Customer'.";
            var user = _db.Users.Find(userId);
            ViewData["User"] = user;
            return Page();
        }

        // Step 3: Get user from database
        var existingUser = _db.Users.Find(userId);
        if (existingUser == null)
        {
            return NotFound();
        }

        // Step 4: Check if email already exists (excluding current user)
        var emailExists = _db.Users.FirstOrDefault(u => u.Email == email && u.UserID != userId);
        if (emailExists != null)
        {
            ViewData["ErrorMessage"] = "Email already exists. Please use a different email.";
            ViewData["User"] = existingUser;
            return Page();
        }

        // Step 5: Update user information
        existingUser.Name = name;
        existingUser.Email = email;
        existingUser.Phone = phone;
        existingUser.Role = role;

        // Step 6: Update password only if a new one was provided
        if (!string.IsNullOrWhiteSpace(password))
        {
            // Validate password length
            if (password.Length < 6)
            {
                ViewData["ErrorMessage"] = "Password must be at least 6 characters long.";
                ViewData["User"] = existingUser;
                return Page();
            }
            existingUser.Password = password;  // In a real application, this should be hashed!
        }
        // If password is empty, keep the existing password (don't change it)

        // Step 7: Save changes to database
        _db.SaveChanges();

        // Step 8: Show success message and redirect to user list
        TempData["SuccessMessage"] = "User updated successfully!";
        return RedirectToPage("./Index");
    }
}

