using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages.Admin.Products;

// This page allows admin to edit an existing product
public class EditModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;
    
    // WebHostEnvironment is needed to save uploaded image files
    private readonly IWebHostEnvironment _environment;

    // Constructor
    public EditModel(AppDbContext db, IWebHostEnvironment environment)
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

        // Step 3: Get all categories for the dropdown
        var categories = _db.Categories.OrderBy(c => c.Name).ToList();

        // Step 4: Store data in ViewData so the .cshtml file can use it
        ViewData["Product"] = product;
        ViewData["Categories"] = categories;

        return Page();
    }

    // This method runs when the form is submitted (POST request)
    // Parameters come from the form fields
    public IActionResult OnPost(int productId, string name, string description, decimal price, int stock, int categoryId, IFormFile imageFile)
    {
        // Step 1: Get product from database first (needed for validation errors)
        var product = _db.Products.Find(productId);
        if (product == null)
        {
            return NotFound();
        }

        // Step 2: Validate required fields
        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Product name is required.";
            var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Product"] = productWithCategory;
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 3: Validate price (must be greater than 0)
        if (price <= 0)
        {
            ViewData["ErrorMessage"] = "Product price must be greater than 0.";
            var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Product"] = productWithCategory;
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 4: Validate stock (must be 0 or greater)
        if (stock < 0)
        {
            ViewData["ErrorMessage"] = "Product stock cannot be negative.";
            var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Product"] = productWithCategory;
            ViewData["Categories"] = categories;
            return Page();
        }
        if (product == null)
        {
            return NotFound();
        }

        // Step 5: Validate category exists
        var selectedCategory = _db.Categories.Find(categoryId);
        if (selectedCategory == null)
        {
            ViewData["ErrorMessage"] = "Selected category does not exist.";
            var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Product"] = productWithCategory;
            ViewData["Categories"] = categories;
            return Page();
        }

        // Step 6: Handle image upload if a new file is provided
        if (imageFile != null && imageFile.Length > 0)
        {
            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                ViewData["ErrorMessage"] = "Only JPG, PNG, and GIF images are allowed.";
                var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
                var categories = _db.Categories.OrderBy(c => c.Name).ToList();
                ViewData["Product"] = productWithCategory;
                ViewData["Categories"] = categories;
                return Page();
            }

            // Validate file size (maximum 5MB)
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                ViewData["ErrorMessage"] = "Image size must be less than 5MB.";
                var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
                var categories = _db.Categories.OrderBy(c => c.Name).ToList();
                ViewData["Product"] = productWithCategory;
                ViewData["Categories"] = categories;
                return Page();
            }

            // Use original filename (must match database)
            var fileName = imageFile.FileName;
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            
            // Create folder if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Full path where image will be saved
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save the image file to disk first (before deleting old one)
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                // Only delete old image AFTER new image is successfully saved
                if (!string.IsNullOrEmpty(product.ImageName) && product.ImageName != fileName)
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, "images", "products", product.ImageName);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Update image name in database
                product.ImageName = fileName;
            }
            catch (Exception ex)
            {
                // If image save fails, don't delete old image
                ViewData["ErrorMessage"] = $"Failed to save image: {ex.Message}";
                var productWithCategory = _db.Products.Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
                var categories = _db.Categories.OrderBy(c => c.Name).ToList();
                ViewData["Product"] = productWithCategory;
                ViewData["Categories"] = categories;
                return Page();
            }
        }
        // If no new image provided, keep the existing image (don't change ImageName)

        // Step 7: Update product information
        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.Stock = stock;
        product.CategoryID = categoryId;

        // Step 8: Save changes to database
        _db.SaveChanges();

        // Step 9: Show success message and redirect to product list
        TempData["SuccessMessage"] = "Product updated successfully!";
        return RedirectToPage("./Index");
    }
}

