using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

[EnableRateLimiting("forms")]
public class AddReviewModel : PageModel
{
    private readonly AppDbContext _db;

    public AddReviewModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet(int productId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToPage("/login");
        }

        var product = _db.Products.Find(productId);
        if (product == null)
        {
            return NotFound();
        }

        ViewData["Product"] = product;
        return Page();
    }

    public IActionResult OnPost(int productId, int rating, string? comment)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Rating must be between 1 and 5.";
            return RedirectToPage("/AddReview", new { productId = productId });
        }

        var trimmedComment = comment?.Trim();
        if (!string.IsNullOrEmpty(trimmedComment) && trimmedComment.Length > 1000)
        {
            TempData["ErrorMessage"] = "Review comment must not exceed 1000 characters.";
            return RedirectToPage("/AddReview", new { productId = productId });
        }

        var product = _db.Products.Find(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("/Products");
        }

        var existingReview = _db.Reviews
            .FirstOrDefault(r => r.UserID == userId && r.ProductID == productId);

        if (existingReview != null)
        {
            existingReview.Rating = rating;
            existingReview.Comment = trimmedComment;
            existingReview.ReviewDate = DateTime.UtcNow;
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Review updated successfully.";
        }
        else
        {
            var newReview = new Review
            {
                UserID = userId,
                ProductID = productId,
                Rating = rating,
                Comment = trimmedComment,
                ReviewDate = DateTime.UtcNow
            };

            _db.Reviews.Add(newReview);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Review submitted successfully.";
        }

        return RedirectToPage("/details", new { id = productId });
    }
}
