using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

public class AddToCartModel : PageModel
{
    private readonly AppDbContext _db;

    public AddToCartModel(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Products");
    }

    public IActionResult OnPost(int? productId, int? quantity)
    {
        if (productId == null)
        {
            var productIdStr = Request.Form["productId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(productIdStr) && int.TryParse(productIdStr, out int parsedProductId))
            {
                productId = parsedProductId;
            }
        }

        if (productId == null || productId <= 0)
        {
            TempData["ErrorMessage"] = "Product ID is required. Please try again.";
            return RedirectToPage("/Products");
        }

        int qty = quantity ?? 1;
        if (qty < 1 || qty > 999)
        {
            TempData["ErrorMessage"] = "Quantity must be at least 1.";
            return RedirectToPage("/Products");
        }

        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            TempData["ErrorMessage"] = "Please sign in to add items to your bag.";
            return RedirectToPage("/login");
        }

        var product = _db.Products.Find(productId.Value);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("/Products");
        }

        if (product.Stock <= 0)
        {
            TempData["ErrorMessage"] = "This product is currently out of stock.";
            return RedirectToPage("/Products");
        }

        if (qty > product.Stock)
        {
            TempData["ErrorMessage"] = $"Only {product.Stock} items available in stock.";
            return RedirectToPage("/Products");
        }

        var cart = _db.Carts.FirstOrDefault(c => c.UserID == userId);
        if (cart == null)
        {
            cart = new Cart { UserID = userId };
            _db.Carts.Add(cart);
            _db.SaveChanges();
        }

        var existingCartItem = _db.CartItems
            .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId.Value);

        if (existingCartItem != null)
        {
            var newQuantity = existingCartItem.Quantity + qty;
            if (newQuantity > product.Stock)
            {
                TempData["ErrorMessage"] = $"Only {product.Stock} items available in stock. You already have {existingCartItem.Quantity} in your bag.";
                return RedirectToPage("/cart");
            }

            existingCartItem.Quantity = newQuantity;
        }
        else
        {
            var cartItem = new CartItem
            {
                CartID = cart.CartID,
                ProductID = productId.Value,
                Quantity = qty
            };
            _db.CartItems.Add(cartItem);
        }

        _db.SaveChanges();

        TempData["SuccessMessage"] = $"'{product.Name}' added to your bag.";
        return RedirectToPage("/cart");
    }
}
