using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using StyleSphere.Data;
using StyleSphere.Models;
using StyleSphere.Services;

namespace StyleSphere.Pages;

[EnableRateLimiting("auth")]
public class ForgotPasswordModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public ForgotPasswordModel(AppDbContext db, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _env = env;
    }

    public void OnGet()
    {
    }

    public IActionResult OnPost(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || !new EmailAddressAttribute().IsValid(email.Trim()))
        {
            ViewData["ErrorMessage"] = "Please enter a valid email address.";
            return Page();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);

        if (user != null)
        {
            // Invalidate any prior unused tokens for this user
            var oldTokens = _db.PasswordResetTokens
                .Where(t => t.UserID == user.UserID && !t.IsUsed)
                .ToList();
            foreach (var old in oldTokens)
            {
                old.IsUsed = true;
            }

            int expiryMinutes = _config.GetValue<int>("Security:PasswordResetTokenExpiryMinutes", 15);
            var (rawToken, tokenHash) = PasswordHasher.GenerateResetToken();

            var resetToken = new PasswordResetToken
            {
                UserID = user.UserID,
                TokenHash = tokenHash, // Stored hashed (SHA-256), never plaintext
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.PasswordResetTokens.Add(resetToken);
            _db.SaveChanges();

            // In Development environment only, expose reset link for local testing when no SMTP provider is configured
            if (_env.IsDevelopment())
            {
                ViewData["DevResetToken"] = rawToken;
            }
        }

        ViewData["SuccessMessage"] = "If an account exists for that email address, password reset instructions have been issued.";
        return Page();
    }
}
