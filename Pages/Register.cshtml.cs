using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page handles user registration (creating new accounts)
public class RegisterModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public RegisterModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    // Just show the registration form
    public void OnGet()
    {
        // No special logic needed, just show the page
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(string name, string email, string phone, string password, string confirmPassword)
    {
        // Step 1: Validate that required fields are provided
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ViewData["ErrorMessage"] = "Please fill in all required fields.";
            return Page();
        }

        // Step 2: Check if passwords match
        if (password != confirmPassword)
        {
            ViewData["ErrorMessage"] = "Passwords do not match. Please try again.";
            return Page();
        }

        // Step 3: Check password length (minimum 6 characters)
        if (password.Length < 6)
        {
            ViewData["ErrorMessage"] = "Password must be at least 6 characters long.";
            return Page();
        }

        // Step 4: Check if email already exists in database
        var existingUser = _db.Users.FirstOrDefault(u => u.Email == email);
        if (existingUser != null)
        {
            ViewData["ErrorMessage"] = "Email already exists. Please use a different email or login.";
            return Page();
        }

        // Step 5: Create new user
        var newUser = new User
        {
            Name = name,
            Email = email,
            Phone = phone,  // Phone is optional, so it can be empty
            Password = password,  // In a real application, this should be hashed!
            Role = "Customer"  // All new users are customers by default
        };

        // Step 6: Add user to database and save
        _db.Users.Add(newUser);
        _db.SaveChanges();

        // Step 7: Store user information in session (auto-login after registration)
        HttpContext.Session.SetString("UserId", newUser.UserID.ToString());
        HttpContext.Session.SetString("UserName", newUser.Name);
        HttpContext.Session.SetString("UserRole", newUser.Role);

        // Step 8: Show success message and redirect to home page
        TempData["SuccessMessage"] = $"Welcome to StyleSphere, {newUser.Name}! Your account has been created successfully.";
        return RedirectToPage("/Index");
    }
}

