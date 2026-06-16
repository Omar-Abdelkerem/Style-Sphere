using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages;

// This page allows users to change their password
public class ChangePasswordModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public ChangePasswordModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public IActionResult OnGet()
    {
        // Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            // User not logged in - redirect to login page
            return RedirectToPage("/login");
        }
        return Page();
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(string oldPassword, string newPassword, string confirmPassword)
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            ViewData["ErrorMessage"] = "You must be logged in to change your password.";
            return Page();
        }

        // Step 2: Validate that all fields are provided
        if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ViewData["ErrorMessage"] = "Please fill in all fields.";
            return Page();
        }

        // Step 3: Check if new passwords match
        if (newPassword != confirmPassword)
        {
            ViewData["ErrorMessage"] = "New password and confirmation do not match.";
            return Page();
        }

        // Step 4: Check new password length (minimum 6 characters)
        if (newPassword.Length < 6)
        {
            ViewData["ErrorMessage"] = "New password must be at least 6 characters long.";
            return Page();
        }

        // Step 5: Get user from database
        var user = _db.Users.Find(userId);
        if (user == null)
        {
            ViewData["ErrorMessage"] = "User not found.";
            return Page();
        }

        // Step 6: Verify old password is correct
        if (user.Password != oldPassword)
        {
            ViewData["ErrorMessage"] = "Current password is incorrect. Please try again.";
            return Page();
        }

        // Step 7: Update password
        user.Password = newPassword;  // In a real application, this should be hashed!
        _db.SaveChanges();

        // Step 8: Show success message
        ViewData["SuccessMessage"] = "Password changed successfully!";
        return Page();
    }
}

