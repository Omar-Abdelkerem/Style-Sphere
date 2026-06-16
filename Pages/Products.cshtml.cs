using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page displays all products, optionally filtered by category
public class ProductsModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public ProductsModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads
    // categoryId is optional - if provided, filter products by that category
    public void OnGet(int? categoryId)
    {
        try
        {
            // Step 1: Get all categories from database
            // Store in ViewData so the .cshtml file can use it
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Categories"] = categories;

            // Step 2: Get products
            List<Product> products;
            
            if (categoryId.HasValue)
            {
                // If categoryId was provided, get only products in that category
                products = _db.Products
                    .Include(p => p.Category)  // Include category information
                    .Where(p => p.CategoryID == categoryId.Value)  // Filter by category
                    .ToList();
            }
            else
            {
                // If no categoryId, get all products
                products = _db.Products
                    .Include(p => p.Category)  // Include category information
                    .ToList();
            }

            // Step 3: Store products in ViewData
            ViewData["Products"] = products;
            
            // Step 4: Store selected category ID (if any)
            ViewData["SelectedCategoryId"] = categoryId;
        }
        catch (Exception ex)
        {
            // If database connection fails, show empty lists
            // This prevents the page from crashing
            ViewData["Categories"] = new List<Category>();
            ViewData["Products"] = new List<Product>();
            ViewData["SelectedCategoryId"] = null;
            ViewData["ErrorMessage"] = "Unable to connect to database. Please check your database connection.";
            // Log the error
            Console.WriteLine("Database error in Products.OnGet: " + ex.Message);
        }
    }
}
