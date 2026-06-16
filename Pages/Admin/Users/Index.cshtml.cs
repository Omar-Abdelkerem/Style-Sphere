using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages.Admin.Users;

// This page displays all users for admin management
public class IndexModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public void OnGet()
    {
        // Step 1: Get all users from database
        // Order by name for easier viewing
        var users = _db.Users.OrderBy(u => u.Name).ToList();

        // Step 2: Store users in ViewData so the .cshtml file can use them
        ViewData["Users"] = users;
    }
}

