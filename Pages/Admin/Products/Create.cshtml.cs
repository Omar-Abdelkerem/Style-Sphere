using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;
using StyleSphere.Services;

namespace StyleSphere.Pages.Admin.Products;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public CreateModel(AppDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }

    public IActionResult OnGet()
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        LoadCategories();
        return Page();
    }

    public IActionResult OnPost(string? name, string? description, decimal price, int stock, int categoryId, IFormFile? imageFile)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin")
        {
            return RedirectToPage("/login");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ViewData["ErrorMessage"] = "Product name is required.";
            LoadCategories();
            return Page();
        }

        var trimmedName = name.Trim();
        var trimmedDesc = description?.Trim();

        if (trimmedName.Length > 100)
        {
            ViewData["ErrorMessage"] = "Product name must not exceed 100 characters.";
            LoadCategories();
            return Page();
        }

        if (!string.IsNullOrEmpty(trimmedDesc) && trimmedDesc.Length > 1000)
        {
            ViewData["ErrorMessage"] = "Product description must not exceed 1000 characters.";
            LoadCategories();
            return Page();
        }

        if (price <= 0 || price > 999999.99m)
        {
            ViewData["ErrorMessage"] = "Product price must be between $0.01 and $999,999.99.";
            LoadCategories();
            return Page();
        }

        if (stock < 0 || stock > 100000)
        {
            ViewData["ErrorMessage"] = "Product stock must be between 0 and 100,000.";
            LoadCategories();
            return Page();
        }

        var category = _db.Categories.Find(categoryId);
        if (category == null)
        {
            ViewData["ErrorMessage"] = "Selected category does not exist.";
            LoadCategories();
            return Page();
        }

        string? imageName = null;
        if (imageFile != null && imageFile.Length > 0)
        {
            var (isValid, errorMsg, safeExt) = ImageUploadValidator.ValidateImage(imageFile);
            if (!isValid || safeExt == null)
            {
                ViewData["ErrorMessage"] = errorMsg ?? "Invalid image file.";
                LoadCategories();
                return Page();
            }

            imageName = ImageUploadValidator.GenerateSafeFileName(safeExt);
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, imageName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }
        }

        var newProduct = new Product
        {
            Name = trimmedName,
            Description = trimmedDesc,
            Price = price,
            Stock = stock,
            CategoryID = categoryId,
            ImageName = imageName
        };

        _db.Products.Add(newProduct);
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Product created successfully.";
        return RedirectToPage("./Index");
    }

    private void LoadCategories()
    {
        ViewData["Categories"] = _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
    }
}
