using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Services;

namespace StyleSphere.Pages.Admin.Products;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EditModel> _logger;

    public EditModel(AppDbContext db, IWebHostEnvironment environment, ILogger<EditModel> logger)
    {
        _db = db;
        _environment = environment;
        _logger = logger;
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

        var product = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefault(p => p.ProductID == id.Value);

        if (product == null)
        {
            return NotFound();
        }

        ViewData["Product"] = product;
        ViewData["Categories"] = _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
        return Page();
    }

    public IActionResult OnPost(int productId, string? name, string? description, decimal price, int stock, int categoryId, IFormFile? imageFile)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        var product = _db.Products.Find(productId);
        if (product == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            ViewData["ErrorMessage"] = "Product name is required and must not exceed 100 characters.";
            ReloadViewData(productId);
            return Page();
        }

        if (!string.IsNullOrEmpty(description) && description.Trim().Length > 1000)
        {
            ViewData["ErrorMessage"] = "Product description must not exceed 1000 characters.";
            ReloadViewData(productId);
            return Page();
        }

        if (price <= 0 || price > 999999.99m)
        {
            ViewData["ErrorMessage"] = "Product price must be between $0.01 and $999,999.99.";
            ReloadViewData(productId);
            return Page();
        }

        if (stock < 0 || stock > 100000)
        {
            ViewData["ErrorMessage"] = "Product stock must be between 0 and 100,000.";
            ReloadViewData(productId);
            return Page();
        }

        var selectedCategory = _db.Categories.Find(categoryId);
        if (selectedCategory == null)
        {
            ViewData["ErrorMessage"] = "Selected category does not exist.";
            ReloadViewData(productId);
            return Page();
        }

        if (imageFile != null && imageFile.Length > 0)
        {
            var (isValid, errorMsg, safeExt) = ImageUploadValidator.ValidateImage(imageFile);
            if (!isValid || safeExt == null)
            {
                ViewData["ErrorMessage"] = errorMsg ?? "Invalid image file.";
                ReloadViewData(productId);
                return Page();
            }

            var fileName = ImageUploadValidator.GenerateSafeFileName(safeExt);
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }

                // Only delete dynamically generated old uploads (starting with "prod-"), keep seeded catalog photos safe
                if (!string.IsNullOrEmpty(product.ImageName) &&
                    product.ImageName.StartsWith("prod-", StringComparison.OrdinalIgnoreCase) &&
                    product.ImageName != fileName)
                {
                    var oldImagePath = Path.Combine(uploadsFolder, Path.GetFileName(product.ImageName));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                product.ImageName = fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save product image for product {ProductId}", productId);
                ViewData["ErrorMessage"] = "Unable to save the uploaded image file. Please try again.";
                ReloadViewData(productId);
                return Page();
            }
        }

        product.Name = name.Trim();
        product.Description = description?.Trim();
        product.Price = price;
        product.Stock = stock;
        product.CategoryID = categoryId;

        _db.SaveChanges();

        TempData["SuccessMessage"] = "Product updated successfully.";
        return RedirectToPage("./Index");
    }

    private void ReloadViewData(int productId)
    {
        ViewData["Product"] = _db.Products.AsNoTracking().Include(p => p.Category).FirstOrDefault(p => p.ProductID == productId);
        ViewData["Categories"] = _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
    }
}
