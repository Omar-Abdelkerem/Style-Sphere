using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Services;

public class AuthPageFilter : IPageFilter
{
    private readonly AppDbContext _db;

    private static readonly HashSet<string> ProtectedCustomerPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "/cart",
        "/PlaceOrder",
        "/Orders",
        "/ChangePassword",
        "/AddReview",
        "/AddToCart"
    };

    public AuthPageFilter(AppDbContext db)
    {
        _db = db;
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var httpContext = context.HttpContext;
        var pagePath = context.ActionDescriptor.ViewEnginePath ?? string.Empty;

        var userIdStr = httpContext.Session.GetString("UserId");
        var sessionVersionStr = httpContext.Session.GetString("SessionVersion");

        bool isValidSession = false;
        string? currentRole = null;
        int currentUserId = 0;

        if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out currentUserId))
        {
            var dbUser = _db.Users.AsNoTracking().FirstOrDefault(u => u.UserID == currentUserId);
            int.TryParse(sessionVersionStr, out int sessionVer);

            if (dbUser != null && sessionVer == dbUser.SessionVersion)
            {
                isValidSession = true;
                currentRole = dbUser.Role;
                // Keep session role/name synchronized with DB
                httpContext.Session.SetString("UserRole", dbUser.Role);
                httpContext.Session.SetString("UserName", dbUser.Name);

                // Populate active cart item count for the layout badge
                int cartCount = _db.Carts
                    .AsNoTracking()
                    .Where(c => c.UserID == currentUserId)
                    .SelectMany(c => c.CartItems)
                    .Sum(ci => (int?)ci.Quantity) ?? 0;
                httpContext.Items["CartItemCount"] = cartCount;
            }
            else
            {
                // Session version mismatch or deleted user: immediately invalidate session
                httpContext.Session.Clear();
            }
        }

        bool isAdminRoute = pagePath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase);
        bool isProtectedCustomerRoute = ProtectedCustomerPages.Contains(pagePath);

        if (isAdminRoute || isProtectedCustomerRoute || isValidSession)
        {
            httpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            httpContext.Response.Headers["Pragma"] = "no-cache";
            httpContext.Response.Headers["Vary"] = "Cookie";
        }

        if (isAdminRoute)
        {
            if (!isValidSession)
            {
                context.Result = new RedirectToPageResult("/login");
                return;
            }

            if (!string.Equals(currentRole, "Admin", StringComparison.Ordinal))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }
        }
        else if (isProtectedCustomerRoute)
        {
            if (!isValidSession)
            {
                context.Result = new RedirectToPageResult("/login");
                return;
            }
        }
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }
}
