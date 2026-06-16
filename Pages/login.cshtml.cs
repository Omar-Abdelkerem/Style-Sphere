using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages;

// This page handles user login
public class loginModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor: receives database connection
    public loginModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    // Just show the login form
    public void OnGet()
    {
        // No special logic needed, just show the page
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields (email and password)
    public IActionResult OnPost(string email, string password)
    {
        // Step 1: Validate that email and password were provided
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewData["ErrorMessage"] = "Please enter both email and password.";
            return Page();  // Show the page again with error message
        }

        // Step 2: Search for user in database
        // Look for a user where email AND password match
        var user = _db.Users
            .FirstOrDefault(u => u.Email == email && u.Password == password);

        // Step 3: Check if user was found
        if (user == null)
        {
            // User not found - wrong email or password
            ViewData["ErrorMessage"] = "Invalid email or password. Please try again.";
            return Page();  // Show the page again with error message
        }

        // Step 4: User found! Store user information in session
        // Session is like a temporary storage that remembers who is logged in
        HttpContext.Session.SetString("UserId", user.UserID.ToString());
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserRole", user.Role);

        // Step 5: Redirect based on user role
        if (user.Role == "Admin")
        {
            // Admin users go to admin dashboard
            return RedirectToPage("/Admin/Index");
        }
        else
        {
            // Regular customers go to home page
            return RedirectToPage("/Index");
        }
    }
}
