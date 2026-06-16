using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

// This page handles placing an order from the shopping cart
public class PlaceOrderModel : PageModel
{
    // Database connection
    private readonly AppDbContext _db;

    // Constructor
    public PlaceOrderModel(AppDbContext db)
    {
        _db = db;
    }

    // This method runs when the page loads (GET request)
    public IActionResult OnGet()
    {
        // Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToPage("/login");
        }

        // Get user's cart to show order summary
        int userId = int.Parse(userIdStr);
        var cart = _db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserID == userId);

        // Calculate totals
        decimal subtotal = 0;
        if (cart != null && cart.CartItems != null)
        {
            foreach (var item in cart.CartItems)
            {
                if (item.Product != null)
                {
                    subtotal += item.Product.Price * item.Quantity;
                }
            }
        }

        decimal tax = subtotal * 0.14m;
        decimal total = subtotal + tax;

        ViewData["Subtotal"] = subtotal;
        ViewData["Tax"] = tax;
        ViewData["Total"] = total;

        return Page();
    }

    // This method runs when the form is submitted (POST request)
    // paymentMethod parameter comes from the form
    public IActionResult OnPost(string paymentMethod)
    {
        // Step 1: Check if user is logged in
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        // Step 2: Validate payment method
        if (string.IsNullOrEmpty(paymentMethod))
        {
            TempData["ErrorMessage"] = "Please select a payment method.";
            return Page();
        }

        // Step 3: Get user's cart with all items
        var cart = _db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserID == userId);

        // Step 4: Check if cart exists and has items
        if (cart == null || cart.CartItems == null || cart.CartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Your cart is empty. Please add items to your cart first.";
            return RedirectToPage("/cart");
        }

        // Step 5: Validate stock availability for all items
        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product == null)
            {
                TempData["ErrorMessage"] = "One or more products are no longer available.";
                return RedirectToPage("/cart");
            }

            if (cartItem.Quantity > cartItem.Product.Stock)
            {
                TempData["ErrorMessage"] = $"Insufficient stock for {cartItem.Product.Name}. Only {cartItem.Product.Stock} available.";
                return RedirectToPage("/cart");
            }
        }

        // Step 6: Calculate order total
        decimal orderTotal = 0;
        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product != null)
            {
                orderTotal += cartItem.Product.Price * cartItem.Quantity;
            }
        }

        // Step 7: Create new order
        var order = new Order
        {
            UserID = userId,
            OrderDate = DateTime.Now,
            Status = "Processing",
            TotalAmount = orderTotal
        };
        _db.Orders.Add(order);
        _db.SaveChanges();  // Save to get OrderID

        // Step 8: Create order items and update product stock
        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product != null)
            {
                // Create order item (snapshot of price at time of order)
                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = cartItem.ProductID,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price  // Store price at time of order
                };
                _db.OrderItems.Add(orderItem);

                // Update product stock (reduce by quantity ordered)
                cartItem.Product.Stock -= cartItem.Quantity;
            }
        }

        // Step 9: Create payment record
        var payment = new Payment
        {
            OrderID = order.OrderID,
            Amount = orderTotal,
            PaymentDate = DateTime.Now,
            Method = paymentMethod,
            Status = "Completed"
        };
        _db.Payments.Add(payment);

        // Step 10: Clear the cart (remove all cart items)
        _db.CartItems.RemoveRange(cart.CartItems);

        // Step 11: Save all changes to database
        _db.SaveChanges();

        // Step 12: Show success message and redirect to orders page
        TempData["SuccessMessage"] = "Order placed successfully!";
        return RedirectToPage("/Orders");
    }
}

