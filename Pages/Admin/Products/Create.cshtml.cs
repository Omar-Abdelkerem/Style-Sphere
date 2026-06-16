using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Products;

// This page allows admin to create a new product
public class CreateModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;
    
    // WebHostEnvironment is needed to save uploaded image files
    private readonly IWebHostEnvironment _environment;

    // Constructor
    public CreateModel(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    // This method runs when the page loads (GET request)
    public void OnGet()
    {
        // Step 1: Get all categories from database
        // This is needed to populate the category dropdown
        var categories = _db.Categories.OrderBy(c => c.Name).ToList();

        // Step 2: Store categories in ViewData so the .cshtml file can use them
        ViewData["Categories"] = categories;
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(string name, string description, decimal price, int stock, int categoryId, IFormFile imageFile)
    {
        // Step 1: Validate required fields
        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Product name is required.";
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 2: Validate price (must be greater than 0)
        if (price <= 0)
        {
            ViewData["ErrorMessage"] = "Product price must be greater than 0.";
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 3: Validate stock (must be 0 or greater)
        if (stock < 0)
        {
            ViewData["ErrorMessage"] = "Product stock cannot be negative.";
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 4: Validate category exists
        var category = _db.Categories.Find(categoryId);
        if (category == null)
        {
            ViewData["ErrorMessage"] = "Selected category does not exist.";
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 5: Handle image upload if provided
        string? imageName = null;
        if (imageFile != null && imageFile.Length > 0)
        {
            // Validate file type (only allow image files)
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                ViewData["ErrorMessage"] = "Only JPG, PNG, and GIF images are allowed.";
                var categories = _db.Categories.OrderBy(c => c.Name).ToList();
                ViewData["Categories"] = categories;
                return Page();
            }

            // Validate file size (maximum 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ViewData["ErrorMessage"] = "Image size must be less than 5MB.";
                var categories = _db.Categories.OrderBy(c => c.Name).ToList();
                ViewData["Categories"] = categories;
                return Page();
            }

            // Use original filename (must match database)
            imageName = imageFile.FileName;
            
            // Path to save the image file
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            
            // Create folder if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Full path where image will be saved
            var filePath = Path.Combine(uploadsFolder, imageName);

            // Save the image file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }
        }

        // Step 6: Create new product
        var newProduct = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            CategoryID = categoryId,
            ImageName = imageName  // Store image filename in database
        };

        // Step 7: Add product to database and save
        _db.Products.Add(newProduct);
        _db.SaveChanges();

        // Step 8: Show success message and redirect to product list
        TempData["SuccessMessage"] = "Product created successfully!";
        return RedirectToPage("./Index");
    }
}

