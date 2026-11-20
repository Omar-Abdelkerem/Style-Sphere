using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

namespace StyleSphere.Pages;

public class cartModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<cartModel> _logger;

    public cartModel(AppDbContext db, IConfiguration config, ILogger<cartModel> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        try
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Please sign in to view your shopping bag.";
                return RedirectToPage("/login");
            }

            var cart = _db.Carts
                .AsNoTracking()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p!.Category)
                .FirstOrDefault(c => c.UserID == userId);

            decimal subtotal = 0;
            if (cart?.CartItems != null)
            {
                foreach (var item in cart.CartItems)
                {
                    if (item.Product != null)
                    {
                        subtotal += item.Product.Price * item.Quantity;
                    }
                }
            }

            decimal taxRate = _config.GetValue<decimal>("StoreSettings:TaxRate", 0.14m);
            decimal tax = Math.Round(subtotal * taxRate, 2);
            decimal total = subtotal + tax;

            ViewData["Cart"] = cart;
            ViewData["Subtotal"] = subtotal;
            ViewData["Tax"] = tax;
            ViewData["Total"] = total;

            return Page();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to load your shopping bag right now. Please try again.";
            _logger.LogError(ex, "Database error in cart.OnGet");
            return RedirectToPage("/Index");
        }
    }

    public IActionResult OnPostUpdateQuantity(int productId, int quantity)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        if (quantity < 1 || quantity > 999)
        {
            TempData["ErrorMessage"] = "Quantity must be between 1 and 999.";
            return RedirectToPage();
        }

        var cart = _db.Carts.FirstOrDefault(c => c.UserID == userId);
        if (cart == null)
        {
            TempData["ErrorMessage"] = "Shopping bag not found.";
            return RedirectToPage();
        }

        var cartItem = _db.CartItems
            .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);

        if (cartItem == null)
        {
            TempData["ErrorMessage"] = "Item not found in your shopping bag.";
            return RedirectToPage();
        }

        var product = _db.Products.Find(productId);
        if (product == null || quantity > product.Stock)
        {
            TempData["ErrorMessage"] = $"Only {product?.Stock ?? 0} units available in stock.";
            return RedirectToPage();
        }

        cartItem.Quantity = quantity;
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Shopping bag updated.";
        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int productId)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        var cart = _db.Carts.FirstOrDefault(c => c.UserID == userId);
        if (cart == null)
        {
            TempData["ErrorMessage"] = "Shopping bag not found.";
            return RedirectToPage();
        }

        var cartItem = _db.CartItems
            .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);

        if (cartItem != null)
        {
            _db.CartItems.Remove(cartItem);
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Item removed from your shopping bag.";
        }

        return RedirectToPage();
    }
}
