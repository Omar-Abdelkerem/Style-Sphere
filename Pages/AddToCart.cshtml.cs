using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page handles adding products to the shopping cart
public class AddToCartModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public AddToCartModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when user clicks "Add to Cart" button (POST request from form)
    // Parameters: productId (which product) and quantity (how many)
    // These come from the form's hidden input fields
    public IActionResult OnPost(int? productId, int? quantity)
    {
        // Step 1: Check if productId was provided
        // Try to get from form data if not in parameter
        if (productId == null)
        {
            // Try to get from Request.Form (POST data)
            var productIdStr = Request.Form["productId"].FirstOrDefault();
            if (!string.IsNullOrEmpty(productIdStr) && int.TryParse(productIdStr, out int parsedProductId))
            {
                productId = parsedProductId;
            }
        }

        if (productId == null)
        {
            TempData["ErrorMessage"] = "Product ID is required. Please try again.";
            return RedirectToPage("/Products");
        }

        // Set default quantity to 1 if not provided
        int qty = quantity ?? 1;

        // Step 2: Check if user is logged in
        // Get user ID from session (stored when user logged in)
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            // User not logged in - redirect to login page
            TempData["ErrorMessage"] = "Please login to add items to cart.";
            return RedirectToPage("/login");
        }

        // Step 3: Get the product from database
        var product = _db.Products.Find(productId.Value);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToPage("/Products");
        }

        // Step 4: Check if product has enough stock
        if (qty > product.Stock)
        {
            TempData["ErrorMessage"] = $"Only {product.Stock} items available in stock.";
            return RedirectToPage("/Products");
        }

        // Step 5: Check if product is in stock
        if (product.Stock <= 0)
        {
            TempData["ErrorMessage"] = "This product is currently out of stock.";
            return RedirectToPage("/Products");
        }

        // Step 6: Get or create user's cart
        // Look for existing cart for this user
        var cart = _db.Carts.FirstOrDefault(c => c.UserID == userId);
        
        if (cart == null)
        {
            // User doesn't have a cart yet - create one
            cart = new Cart { UserID = userId };
            _db.Carts.Add(cart);
            _db.SaveChanges();  // Save to get the CartID
        }

        // Step 7: Check if this product is already in the cart
        var existingCartItem = _db.CartItems
            .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId.Value);

        if (existingCartItem != null)
        {
            // Product already in cart - increase quantity
            var newQuantity = existingCartItem.Quantity + qty;
            
            // Check stock again with new quantity
            if (newQuantity > product.Stock)
            {
                TempData["ErrorMessage"] = $"Only {product.Stock} items available in stock. You already have {existingCartItem.Quantity} in your cart.";
                return RedirectToPage("/cart");
            }
            
            existingCartItem.Quantity = newQuantity;
        }
        else
        {
            // Product not in cart - add new cart item
            var cartItem = new CartItem
            {
                CartID = cart.CartID,
                ProductID = productId.Value,
                Quantity = qty
            };
            _db.CartItems.Add(cartItem);
        }

        // Step 8: Save changes to database
        _db.SaveChanges();

        // Step 9: Show success message and redirect to cart page
        TempData["SuccessMessage"] = $"'{product.Name}' added to cart successfully!";
        return RedirectToPage("/cart");
    }
}

