using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using StyleSphere.Data;
using StyleSphere.Services;

namespace StyleSphere.Pages;

[EnableRateLimiting("auth")]
public class ChangePasswordModel : PageModel
{
    private readonly AppDbContext _db;

    public ChangePasswordModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToPage("/login");
        }
        return Page();
    }

    public IActionResult OnPost(string? oldPassword, string? newPassword, string? confirmPassword)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ViewData["ErrorMessage"] = "Please fill in all password fields.";
            return Page();
        }

        if (newPassword != confirmPassword)
        {
            ViewData["ErrorMessage"] = "New password and confirmation do not match.";
            return Page();
        }

        if (newPassword.Length < 6 || newPassword.Length > 255)
        {
            ViewData["ErrorMessage"] = "New password must be between 6 and 255 characters long.";
            return Page();
        }

        var user = _db.Users.Find(userId);
        if (user == null)
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/login");
        }

        if (!PasswordHasher.VerifyPassword(oldPassword, user.Password))
        {
            ViewData["ErrorMessage"] = "Current password is incorrect. Please try again.";
            return Page();
        }

        // Update password hash and bump SessionVersion to invalidate all other active sessions
        user.Password = PasswordHasher.HashPassword(newPassword);
        user.SessionVersion++;
        _db.SaveChanges();

        // Keep the current session valid by updating its SessionVersion
        HttpContext.Session.SetString("SessionVersion", user.SessionVersion.ToString());

        ViewData["SuccessMessage"] = "Your password has been updated and all other active sessions have been signed out.";
        return Page();
    }
}
