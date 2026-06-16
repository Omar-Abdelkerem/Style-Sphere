using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This is the code-behind file for the Index page
// It handles getting data from the database and passing it to the view
public class IndexModel : PageModel
{
    // This variable will hold our database connection
    private readonly AppDbContext _db;

    // Constructor: This runs when the page is created
    // It receives the database connection (dependency injection)
    public IndexModel(AppDbContext db)
    {
        _db = db; // Store the database connection in our variable
    }

    // This method runs when the page loads (GET request)
    public void OnGet()
    {
        try
        {
            // Step 1: Get all products from the database
            // Include the Category information so we can display category names
            var allProducts = _db.Products
                .Include(p => p.Category)  // Include related Category data
                .ToList();  // Execute the query and get results as a list

            // Step 2: Take only the first 12 products for the homepage
            var featuredProducts = allProducts.Take(12).ToList();

            // Step 3: Store the products in ViewData so the .cshtml file can use them
            // ViewData is like a dictionary that stores data to pass to the view
            ViewData["Products"] = featuredProducts;
        }
        catch (Exception ex)
        {
            // If database connection fails, show empty list
            // This prevents the page from crashing
            ViewData["Products"] = new List<Product>();
            ViewData["ErrorMessage"] = "Unable to connect to database. Please check your database connection.";
            // Log the error (in a real app, you'd use a logger)
            Console.WriteLine("Database error in Index.OnGet: " + ex.Message);
        }
    }
}
