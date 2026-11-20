using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Services;

namespace StyleSphere.Pages;

[EnableRateLimiting("auth")]
public class ResetPasswordModel : PageModel
{
    private readonly AppDbContext _db;

    public ResetPasswordModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            ViewData["ErrorMessage"] = "Invalid or missing password reset token.";
            ViewData["TokenInvalid"] = true;
            return Page();
        }

        string tokenHash = PasswordHasher.HashResetToken(token);
        var record = _db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefault(t => t.TokenHash == tokenHash && !t.IsUsed);

        if (record == null || record.ExpiresAt <= DateTime.UtcNow)
        {
            ViewData["ErrorMessage"] = "This password reset token is invalid or has expired. Please request a new one.";
            ViewData["TokenInvalid"] = true;
            return Page();
        }

        ViewData["Token"] = token;
        return Page();
    }

    public IActionResult OnPost(string? token, string? newPassword, string? confirmPassword)
    {
        ViewData["Token"] = token;

        if (string.IsNullOrWhiteSpace(token))
        {
            ViewData["ErrorMessage"] = "Invalid or missing password reset token.";
            ViewData["TokenInvalid"] = true;
            return Page();
        }

        if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ViewData["ErrorMessage"] = "Please enter and confirm your new password.";
            return Page();
        }

        if (newPassword != confirmPassword)
        {
            ViewData["ErrorMessage"] = "New password and confirmation do not match.";
            return Page();
        }

        if (newPassword.Length < 6 || newPassword.Length > 255)
        {
            ViewData["ErrorMessage"] = "Password must be between 6 and 255 characters long.";
            return Page();
        }

        string tokenHash = PasswordHasher.HashResetToken(token);
        var record = _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefault(t => t.TokenHash == tokenHash && !t.IsUsed);

        if (record == null || record.ExpiresAt <= DateTime.UtcNow || record.User == null)
        {
            ViewData["ErrorMessage"] = "This password reset token is invalid, has already been used, or has expired.";
            ViewData["TokenInvalid"] = true;
            return Page();
        }

        // Single-use: mark token as used immediately
        record.IsUsed = true;

        // Update user's password hash and increment SessionVersion to invalidate all active sessions
        record.User.Password = PasswordHasher.HashPassword(newPassword);
        record.User.SessionVersion++;

        _db.SaveChanges();

        TempData["SuccessMessage"] = "Your password has been reset successfully. Please sign in with your new password.";
        return RedirectToPage("/login");
    }
}
