using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page displays the user's shopping cart
public class cartModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public cartModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public IActionResult OnGet()
    {
        try
        {
            // Step 1: Check if user is logged in
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                TempData["ErrorMessage"] = "Please login to view your cart.";
                return RedirectToPage("/login");
            }

            // Step 2: Get user's cart from database
            // Include CartItems and their Products so we can display product information
            var cart = _db.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefault(c => c.UserID == userId);

            // Step 3: Calculate totals
            decimal subtotal = 0;
            if (cart != null && cart.CartItems != null)
            {
                // Loop through each cart item and calculate total
                foreach (var item in cart.CartItems)
                {
                    if (item.Product != null)
                    {
                        subtotal += item.Product.Price * item.Quantity;
                    }
                }
            }

            decimal tax = subtotal * 0.14m;  // 14% tax
            decimal total = subtotal + tax;

            // Step 4: Store data in ViewData so the .cshtml file can use it
            ViewData["Cart"] = cart;
            ViewData["Subtotal"] = subtotal;
            ViewData["Tax"] = tax;
            ViewData["Total"] = total;

            return Page();
        }
        catch (Exception ex)
        {
            // If database connection fails, show error
            TempData["ErrorMessage"] = "Unable to connect to database. Please try again later.";
            Console.WriteLine("Database error in cart.OnGet: " + ex.Message);
            return RedirectToPage("/Index");
        }
    }

    // This method runs when user updates quantity (POST request)
    // Parameters come from the form
    public IActionResult OnPostUpdateQuantity(int productId, int quantity)
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        // Step 2: Get user's cart
        var cart = _db.Carts.FirstOrDefault(c => c.UserID == userId);
        if (cart == null)
        {
            TempData["ErrorMessage"] = "Cart not found.";
            return RedirectToPage();
        }

        // Step 3: Find the cart item
        var cartItem = _db.CartItems
            .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);
        
        if (cartItem == null)
        {
            TempData["ErrorMessage"] = "Item not found in cart.";
            return RedirectToPage();
        }

        // Step 4: Validate quantity
        var product = _db.Products.Find(productId);
        if (product == null || quantity > product.Stock)
        {
            TempData["ErrorMessage"] = $"Only {product?.Stock ?? 0} items available in stock.";
            return RedirectToPage();
        }

        // Step 5: Update quantity
        cartItem.Quantity = quantity;
        _db.SaveChanges();

        TempData["SuccessMessage"] = "Cart updated successfully.";
        return RedirectToPage();
    }

    // This method runs when user removes an item (POST request)
    public IActionResult OnPostRemove(int productId)
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        // Step 2: Get user's cart
        var cart = _db.Carts.FirstOrDefault(c => c.UserID == userId);
        if (cart == null)
        {
            TempData["ErrorMessage"] = "Cart not found.";
            return RedirectToPage();
        }

        // Step 3: Find and remove the cart item
        var cartItem = _db.CartItems
            .FirstOrDefault(ci => ci.CartID == cart.CartID && ci.ProductID == productId);
        
        if (cartItem != null)
        {
            _db.CartItems.Remove(cartItem);
            _db.SaveChanges();
            TempData["SuccessMessage"] = "Item removed from cart.";
        }

        return RedirectToPage();
    }
}
