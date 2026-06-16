using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Users;

// This page allows admin to create a new user
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
    public IActionResult OnPost(string name, string email, string phone, string password, string role)
    {
        // Step 1: Validate that required fields are provided
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewData["ErrorMessage"] = "Please fill in all required fields (Name, Email, Password).";
            return Page();
        }

        // Step 2: Validate password length (minimum 6 characters)
        if (password.Length < 6)
        {
            ViewData["ErrorMessage"] = "Password must be at least 6 characters long.";
            return Page();
        }

        // Step 3: Validate role (must be Admin or Customer)
        if (string.IsNullOrWhiteSpace(role) || (role != "Admin" && role != "Customer"))
        {
            ViewData["ErrorMessage"] = "Role must be either 'Admin' or 'Customer'.";
            return Page();
        }

        // Step 4: Check if email already exists
        var existingUser = _db.Users.FirstOrDefault(u => u.Email == email);
        if (existingUser != null)
        {
            ViewData["ErrorMessage"] = "Email already exists. Please use a different email.";
            return Page();
        }

        // Step 5: Create new user
        var newUser = new User
        {
            Name = name,
            Email = email,
            Phone = phone,  // Phone is optional, so it can be empty
            Password = password,  // In a real application, this should be hashed!
            Role = role
        };

        // Step 6: Add user to database and save
        _db.Users.Add(newUser);
        _db.SaveChanges();

        // Step 7: Show success message and redirect to user list
        TempData["SuccessMessage"] = "User created successfully!";
        return RedirectToPage("./Index");
    }
}

