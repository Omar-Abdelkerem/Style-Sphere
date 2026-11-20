using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Models;
using StyleSphere.Services;

namespace StyleSphere.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }
        return Page();
    }

    public IActionResult OnPost(string? name, string? email, string? phone, string? password, string? role)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewData["ErrorMessage"] = "Please fill in all required fields (Name, Email, Password).";
            return Page();
        }

        var trimmedName = name.Trim();
        var trimmedEmail = email.Trim().ToLowerInvariant();
        var trimmedPhone = phone?.Trim();

        if (trimmedName.Length > 100)
        {
            ViewData["ErrorMessage"] = "Name must not exceed 100 characters.";
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

        if (password.Length < 6 || password.Length > 255)
        {
            ViewData["ErrorMessage"] = "Password must be between 6 and 255 characters long.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(role) || (role != "Admin" && role != "Customer"))
        {
            ViewData["ErrorMessage"] = "Role must be either 'Admin' or 'Customer'.";
            return Page();
        }

        var existingUser = _db.Users.FirstOrDefault(u => u.Email.ToLower() == trimmedEmail);
        if (existingUser != null)
        {
            ViewData["ErrorMessage"] = "Email already exists. Please use a different email.";
            return Page();
        }

        var newUser = new User
        {
            Name = trimmedName,
            Email = trimmedEmail,
            Phone = trimmedPhone,
            Password = PasswordHasher.HashPassword(password),
            Role = role,
            SessionVersion = 1
        };

        _db.Users.Add(newUser);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "User account created successfully.";
        return RedirectToPage("./Index");
    }
}
