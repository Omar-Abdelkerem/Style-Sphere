using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page displays detailed information about a single product
public class detailsModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public detailsModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads
    // id parameter comes from the URL (e.g., /details?id=5)
    public IActionResult OnGet(int? id)
    {
        try
        {
            // Step 1: Check if product ID was provided
            if (id == null)
            {
                return NotFound();  // Show 404 error page
            }

            // Step 2: Get product from database
            // Include Category information so we can display category name
            var product = _db.Products
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductID == id.Value);

            // Step 3: Check if product was found
            if (product == null)
            {
                return NotFound();  // Show 404 error page
            }

            // Step 4: Get all reviews for this product
            // Include User information so we can display reviewer names
            var reviews = _db.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductID == id.Value)
                .OrderByDescending(r => r.ReviewDate)
                .ToList();

            // Step 5: Calculate average rating
            decimal averageRating = 0;
            if (reviews.Count > 0)
            {
                // Sum all ratings and divide by count
                int totalRating = 0;
                foreach (var review in reviews)
                {
                    totalRating += review.Rating;
                }
                averageRating = (decimal)totalRating / reviews.Count;
            }

            // Step 6: Store data in ViewData so the .cshtml file can use it
            ViewData["Product"] = product;
            ViewData["Reviews"] = reviews;
            ViewData["AverageRating"] = averageRating;
            ViewData["ReviewCount"] = reviews.Count;

            return Page();
        }
        catch (Exception ex)
        {
            // If database connection fails, show error
            ViewData["ErrorMessage"] = "Unable to connect to database. Please try again later.";
            Console.WriteLine("Database error in details.OnGet: " + ex.Message);
            return Page();
        }
    }
}
