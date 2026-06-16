using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Products;

// This page allows admin to delete a product
public class DeleteModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;
    
    // WebHostEnvironment is needed to delete image files
    private readonly IWebHostEnvironment _environment;

    // Constructor
    public DeleteModel(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    // This method runs when the page loads (GET request)
    // id parameter comes from the URL
    public IActionResult OnGet(int? id)
    {
        // Step 1: Check if product ID was provided
        if (id == null)
        {
            return NotFound();
        }

        // Step 2: Get product from database
        // Include Category information so we can display category name
        var product = _db.Products
            .Include(p => p.Category)
            .FirstOrDefault(p => p.ProductID == id.Value);

        if (product == null)
        {
            return NotFound();
        }

        // Step 3: Store product in ViewData so the .cshtml file can use it
        ViewData["Product"] = product;

        return Page();
    }

    // This method runs when the delete button is clicked (POST request)
    // id parameter comes from the form
    public IActionResult OnPost(int? id)
    {
        // Step 1: Check if product ID was provided
        if (id == null)
        {
            return NotFound();
        }

        // Step 2: Get product from database
        var product = _db.Products.Find(id.Value);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("./Index");
        }

        // Step 3: Delete product from database first
        // (We'll keep the image file - you can manually delete it if needed)
        // This prevents accidental image deletion
        _db.Products.Remove(product);
        _db.SaveChanges();

        // Step 4: Optionally delete image file (commented out to prevent accidental deletion)
        // Uncomment the code below if you want to delete images when products are deleted
        /*
        if (!string.IsNullOrEmpty(product.ImageName))
        {
            var imagePath = Path.Combine(_environment.WebRootPath, "images", "products", product.ImageName);
            if (System.IO.File.Exists(imagePath))
            {
                try
                {
                    System.IO.File.Delete(imagePath);
                }
                catch
                {
                    // If image deletion fails, continue anyway (product is already deleted)
                }
            }
        }
        */

        // Step 5: Show success message and redirect to product list
        TempData["SuccessMessage"] = "Product deleted successfully!";
        return RedirectToPage("./Index");
    }
}

