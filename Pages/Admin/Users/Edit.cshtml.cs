using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Services;

namespace StyleSphere.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet(int? id)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (id == null)
        {
            return NotFound();
        }

        var user = _db.Users.Find(id.Value);
        if (user == null)
        {
            return NotFound();
        }

        ViewData["User"] = user;
        return Page();
    }

    public IActionResult OnPost(int userId, string? name, string? email, string? phone, string? password, string? role)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        var existingUser = _db.Users.Find(userId);
        if (existingUser == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            ViewData["ErrorMessage"] = "Please fill in all required fields (Name, Email).";
            ViewData["User"] = existingUser;
            return Page();
        }

        var trimmedName = name.Trim();
        var trimmedEmail = email.Trim().ToLowerInvariant();
        var trimmedPhone = phone?.Trim();

        if (trimmedName.Length > 100 || trimmedEmail.Length > 100 || !new EmailAddressAttribute().IsValid(trimmedEmail))
        {
            ViewData["ErrorMessage"] = "Please provide a valid name and email address (max 100 characters).";
            ViewData["User"] = existingUser;
            return Page();
        }

        if (!string.IsNullOrEmpty(trimmedPhone) && trimmedPhone.Length > 20)
        {
            ViewData["ErrorMessage"] = "Phone number must not exceed 20 characters.";
            ViewData["User"] = existingUser;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(role) || (role != "Admin" && role != "Customer"))
        {
            ViewData["ErrorMessage"] = "Role must be either 'Admin' or 'Customer'.";
            ViewData["User"] = existingUser;
            return Page();
        }

        var emailExists = _db.Users.FirstOrDefault(u => u.Email.ToLower() == trimmedEmail && u.UserID != userId);
        if (emailExists != null)
        {
            ViewData["ErrorMessage"] = "Email already exists. Please use a different email.";
            ViewData["User"] = existingUser;
            return Page();
        }

        bool invalidateSessions = existingUser.Role != role;

        existingUser.Name = trimmedName;
        existingUser.Email = trimmedEmail;
        existingUser.Phone = trimmedPhone;
        existingUser.Role = role;

        if (!string.IsNullOrWhiteSpace(password))
        {
            if (password.Length < 6 || password.Length > 255)
            {
                ViewData["ErrorMessage"] = "Password must be between 6 and 255 characters long.";
                ViewData["User"] = existingUser;
                return Page();
            }

            existingUser.Password = PasswordHasher.HashPassword(password);
            invalidateSessions = true;
        }

        if (invalidateSessions)
        {
            existingUser.SessionVersion++;

            // If the admin edited their own account, keep their current session valid
            var currentUserIdStr = HttpContext.Session.GetString("UserId");
            if (currentUserIdStr == existingUser.UserID.ToString())
            {
                HttpContext.Session.SetString("SessionVersion", existingUser.SessionVersion.ToString());
                HttpContext.Session.SetString("UserRole", existingUser.Role);
                HttpContext.Session.SetString("UserName", existingUser.Name);
            }
        }

        _db.SaveChanges();

        TempData["SuccessMessage"] = "User account updated successfully.";
        return RedirectToPage("./Index");
    }
}
