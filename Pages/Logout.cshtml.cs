using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;

namespace StyleSphere.Pages;

public class LogoutModel : PageModel
{
    private readonly AppDbContext _db;

    public LogoutModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        // Reject cross-site GET subresource force-logout attempts (CSRF protection)
        var secFetchSite = Request.Headers["Sec-Fetch-Site"].ToString();
        if (string.Equals(secFetchSite, "cross-site", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Cross-site logout requests are not permitted.");
        }

        var referer = Request.Headers.Referer.ToString();
        if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refUri))
        {
            if (!string.Equals(refUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Cross-origin logout requests are not permitted.");
            }
        }

        InvalidateServerSession();
        return RedirectToPage("/Index");
    }

    public IActionResult OnPost()
    {
        InvalidateServerSession();
        return RedirectToPage("/Index");
    }

    private void InvalidateServerSession()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
        {
            var user = _db.Users.Find(userId);
            if (user != null)
            {
                user.SessionVersion++;
                _db.SaveChanges();
            }
        }

        HttpContext.Session.Clear();
        Response.Cookies.Delete("__StyleSphere.Session");
        Response.Cookies.Delete(".AspNetCore.Session");
    }
}
