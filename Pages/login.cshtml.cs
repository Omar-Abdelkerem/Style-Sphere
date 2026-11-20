using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Services;

namespace StyleSphere.Pages;

[EnableRateLimiting("auth")]
public class loginModel : PageModel
{
    private readonly AppDbContext _db;

    public loginModel(AppDbContext db)
    {
        _db = db;
    }

    public void OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            ViewData["ReturnUrl"] = returnUrl;
        }
    }

    public IActionResult OnPost(string? email, string? password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            ViewData["ErrorMessage"] = "Please enter both email and password.";
            ViewData["EnteredEmail"] = email;
            return Page();
        }

        if (email.Length > 100 || password.Length > 255)
        {
            ViewData["ErrorMessage"] = "Invalid email or password. Please try again.";
            return Page();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = _db.Users
            .FirstOrDefault(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || !PasswordHasher.VerifyPassword(password, user.Password))
        {
            ViewData["ErrorMessage"] = "Invalid email or password. Please try again.";
            ViewData["EnteredEmail"] = email.Trim();
            return Page();
        }

        // Transparently upgrade hash if user still had a legacy format
        if (!PasswordHasher.IsHashed(user.Password))
        {
            user.Password = PasswordHasher.HashPassword(password);
            _db.SaveChanges();
        }

        // Prevent session fixation: clear pre-login session state and rotate session token
        HttpContext.Session.Clear();
        HttpContext.Session.SetString("SessionNonce", Guid.NewGuid().ToString("N"));
        HttpContext.Session.SetString("UserId", user.UserID.ToString());
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetString("SessionVersion", user.SessionVersion.ToString());

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        if (user.Role == "Admin")
        {
            return RedirectToPage("/Admin/Index");
        }

        return RedirectToPage("/Index");
    }
}
