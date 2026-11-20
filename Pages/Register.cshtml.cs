using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using StyleSphere.Data;
using StyleSphere.Models;
using StyleSphere.Services;

namespace StyleSphere.Pages;

[EnableRateLimiting("auth")]
public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public RegisterModel(AppDbContext db)
    {
        _db = db;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string? name, string? email, string? phone, string? password, string? confirmPassword)
    {
        ViewData["EnteredName"] = name?.Trim();
        ViewData["EnteredEmail"] = email?.Trim();
        ViewData["EnteredPhone"] = phone?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            ViewData["ErrorMessage"] = "Please fill in all required fields (Full Name, Email Address, and Password).";
            return Page();
        }

        var trimmedName = name.Trim();
        var trimmedEmail = email.Trim().ToLowerInvariant();
        var trimmedPhone = phone?.Trim();

        if (trimmedName.Length > 100)
        {
            ViewData["ErrorMessage"] = "Full Name must not exceed 100 characters.";
            return Page();
        }

        if (trimmedEmail.Length > 100 || !new EmailAddressAttribute().IsValid(trimmedEmail))
        {
            ViewData["ErrorMessage"] = "Please enter a valid email address.";
            return Page();
        }

        if (!string.IsNullOrEmpty(trimmedPhone) && trimmedPhone.Length > 20)
        {
            ViewData["ErrorMessage"] = "Phone number must not exceed 20 characters.";
            return Page();
        }

        if (password != confirmPassword)
        {
            ViewData["ErrorMessage"] = "Passwords do not match. Please try again.";
            return Page();
        }

        if (password.Length < 6 || password.Length > 255)
        {
            ViewData["ErrorMessage"] = "Password must be between 6 and 255 characters long.";
            return Page();
        }

        var existingUser = _db.Users.FirstOrDefault(u => u.Email.ToLower() == trimmedEmail);
        if (existingUser != null)
        {
            ViewData["ErrorMessage"] = "An account with this email already exists. Please sign in or use a different email.";
            return Page();
        }

        var newUser = new User
        {
            Name = trimmedName,
            Email = trimmedEmail,
            Phone = trimmedPhone,
            Password = PasswordHasher.HashPassword(password),
            Role = "Customer",
            SessionVersion = 1
        };

        _db.Users.Add(newUser);
        _db.SaveChanges();

        // Issue fresh session after registration
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("SessionNonce", Guid.NewGuid().ToString("N"));
        HttpContext.Session.SetString("UserId", newUser.UserID.ToString());
        HttpContext.Session.SetString("UserName", newUser.Name);
        HttpContext.Session.SetString("UserRole", newUser.Role);
        HttpContext.Session.SetString("SessionVersion", newUser.SessionVersion.ToString());

        TempData["SuccessMessage"] = $"Welcome to StyleSphere, {newUser.Name}! Your account has been created.";
        return RedirectToPage("/Index");
    }
}
