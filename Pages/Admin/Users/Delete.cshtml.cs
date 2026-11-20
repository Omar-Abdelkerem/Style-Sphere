using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin.Users;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;

    public DeleteModel(AppDbContext db)
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

    public IActionResult OnPost(int? id)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (id == null)
        {
            return NotFound();
        }

        var currentUserIdStr = HttpContext.Session.GetString("UserId");
        if (currentUserIdStr == id.Value.ToString())
        {
            TempData["ErrorMessage"] = "You cannot delete your own active administrator account.";
            return RedirectToPage("./Index");
        }

        var user = _db.Users.Find(id.Value);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToPage("./Index");
        }

        if (_db.Orders.Any(o => o.UserID == id.Value))
        {
            TempData["ErrorMessage"] = $"Cannot delete '{user.Name}' because they have historical orders on record.";
            return RedirectToPage("./Index");
        }

        // Remove user's reviews before deleting user account
        var userReviews = _db.Reviews.Where(r => r.UserID == id.Value).ToList();
        if (userReviews.Count > 0)
        {
            _db.Reviews.RemoveRange(userReviews);
        }

        _db.Users.Remove(user);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "User account deleted successfully.";
        return RedirectToPage("./Index");
    }
}
