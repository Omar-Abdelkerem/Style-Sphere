using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page allows users to add or edit a review for a product
public class AddReviewModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public AddReviewModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    // productId parameter comes from the URL
    public IActionResult OnGet(int productId)
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToPage("/login");
        }

        // Step 2: Get product from database
        var product = _db.Products.Find(productId);
        if (product == null)
        {
            return NotFound();
        }

        // Step 3: Store product in ViewData
        ViewData["Product"] = product;

        return Page();
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(int productId, int rating, string comment)
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        // Step 2: Validate rating (must be between 1 and 5)
        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Rating must be between 1 and 5.";
            return RedirectToPage("/AddReview", new { productId = productId });
        }

        // Step 3: Check if product exists
        var product = _db.Products.Find(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("/Products");
        }

        // Step 4: Check if user already reviewed this product
        var existingReview = _db.Reviews
            .FirstOrDefault(r => r.UserID == userId && r.ProductID == productId);

        if (existingReview != null)
        {
            // User already reviewed - update existing review
            existingReview.Rating = rating;
            existingReview.Comment = comment;
            existingReview.ReviewDate = DateTime.Now;
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Review updated successfully!";
        }
        else
        {
            // User hasn't reviewed yet - create new review
            var newReview = new Review
            {
                UserID = userId,
                ProductID = productId,
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.Now
            };

            _db.Reviews.Add(newReview);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Review added successfully!";
        }

        // Step 5: Redirect back to product details page
        return RedirectToPage("/details", new { id = productId });
    }
}

