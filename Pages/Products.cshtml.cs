using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page displays all products, optionally filtered by category or search term
public class ProductsModel : PageModel
{
    private readonly AppDbContext _db;

    public ProductsModel(AppDbContext db)
    {
        _db = db;
    }

    public void OnGet(int? categoryId, string? search)
    {
        try
        {
            var categories = _db.Categories.OrderBy(c => c.Name).ToList();
            ViewData["Categories"] = categories;

            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term)));
                ViewData["SearchTerm"] = search;
            }

            var products = query.ToList();

            ViewData["Products"] = products;
            ViewData["SelectedCategoryId"] = categoryId;
        }
        catch (Exception ex)
        {
            ViewData["Categories"] = new List<Category>();
            ViewData["Products"] = new List<Product>();
            ViewData["SelectedCategoryId"] = null;
            ViewData["ErrorMessage"] = "Unable to connect to database. Please check your database connection.";
            Console.WriteLine("Database error in Products.OnGet: " + ex.Message);
        }
    }
}
