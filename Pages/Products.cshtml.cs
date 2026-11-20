using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

public class ProductsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ProductsModel> _logger;

    public ProductsModel(AppDbContext db, IConfiguration config, ILogger<ProductsModel> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public void OnGet(int? categoryId, string? search, int pageNumber = 1)
    {
        try
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            int pageSize = _config.GetValue<int>("StoreSettings:PageSize", 12);
            if (pageSize <= 0 || pageSize > 100)
            {
                pageSize = 12;
            }

            var categories = _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToList();
            ViewData["Categories"] = categories;

            var query = _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmedSearch = search.Trim();
                if (trimmedSearch.Length > 100)
                {
                    trimmedSearch = trimmedSearch.Substring(0, 100);
                }

                var term = trimmedSearch.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         (p.Description != null && p.Description.ToLower().Contains(term)));
                ViewData["SearchTerm"] = trimmedSearch;
            }

            int totalCount = query.Count();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages > 0 && pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var products = query
                .OrderBy(p => p.ProductID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewData["Products"] = products;
            ViewData["SelectedCategoryId"] = categoryId;
            ViewData["TotalCount"] = totalCount;
            ViewData["CurrentPage"] = pageNumber;
            ViewData["TotalPages"] = totalPages;
        }
        catch (Exception ex)
        {
            ViewData["Categories"] = new List<Category>();
            ViewData["Products"] = new List<Product>();
            ViewData["SelectedCategoryId"] = null;
            ViewData["TotalCount"] = 0;
            ViewData["CurrentPage"] = 1;
            ViewData["TotalPages"] = 1;
            ViewData["ErrorMessage"] = "Unable to load catalog items at this time.";
            _logger.LogError(ex, "Database error in Products.OnGet");
        }
    }
}
