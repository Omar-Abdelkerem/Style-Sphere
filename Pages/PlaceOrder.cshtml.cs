using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Models;

namespace StyleSphere.Pages;

public class PlaceOrderModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Credit Card",
        "Debit Card",
        "Cash"
    };

    public PlaceOrderModel(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public IActionResult OnGet()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        var cart = _db.Carts
            .AsNoTracking()
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserID == userId);

        if (cart == null || cart.CartItems == null || cart.CartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Your shopping bag is empty.";
            return RedirectToPage("/cart");
        }

        PopulateOrderSummary(cart);
        return Page();
    }

    public IActionResult OnPost(string? paymentMethod, string? deliveryAddress)
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
        {
            return RedirectToPage("/login");
        }

        var cart = _db.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserID == userId);

        if (cart == null || cart.CartItems == null || cart.CartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Your shopping bag is empty. Please add items before checking out.";
            return RedirectToPage("/cart");
        }

        if (string.IsNullOrWhiteSpace(paymentMethod) || !AllowedPaymentMethods.Contains(paymentMethod.Trim()))
        {
            ViewData["ErrorMessage"] = "Please select a valid payment method.";
            PopulateOrderSummary(cart);
            return Page();
        }

        if (!string.IsNullOrWhiteSpace(deliveryAddress) && deliveryAddress.Trim().Length > 255)
        {
            ViewData["ErrorMessage"] = "Delivery address must not exceed 255 characters.";
            PopulateOrderSummary(cart);
            return Page();
        }

        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product == null)
            {
                TempData["ErrorMessage"] = "One or more items in your bag are no longer available.";
                return RedirectToPage("/cart");
            }

            if (cartItem.Quantity <= 0 || cartItem.Quantity > cartItem.Product.Stock)
            {
                TempData["ErrorMessage"] = $"Insufficient stock for {cartItem.Product.Name}. Only {cartItem.Product.Stock} available.";
                return RedirectToPage("/cart");
            }
        }

        decimal subtotal = 0;
        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product != null)
            {
                subtotal += cartItem.Product.Price * cartItem.Quantity;
            }
        }

        decimal taxRate = _config.GetValue<decimal>("StoreSettings:TaxRate", 0.14m);
        decimal tax = Math.Round(subtotal * taxRate, 2);
        decimal orderTotal = subtotal + tax;

        var order = new Order
        {
            UserID = userId,
            OrderDate = DateTime.UtcNow,
            Status = "Processing",
            TotalAmount = orderTotal
        };
        _db.Orders.Add(order);
        _db.SaveChanges();

        foreach (var cartItem in cart.CartItems)
        {
            if (cartItem.Product != null)
            {
                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = cartItem.ProductID,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Product.Price
                };
                _db.OrderItems.Add(orderItem);

                cartItem.Product.Stock -= cartItem.Quantity;
            }
        }

        var payment = new Payment
        {
            OrderID = order.OrderID,
            Amount = orderTotal,
            PaymentDate = DateTime.UtcNow,
            Method = paymentMethod.Trim(),
            Status = "Completed"
        };
        _db.Payments.Add(payment);

        if (!string.IsNullOrWhiteSpace(deliveryAddress))
        {
            var shipper = new Shipper
            {
                Address = deliveryAddress.Trim()
            };
            _db.Shippers.Add(shipper);
            _db.SaveChanges();

            var shipment = new Shipment
            {
                OrderID = order.OrderID,
                ShipperID = shipper.ShipperID,
                Delivery = "Standard Express (3-5 Business Days)"
            };
            _db.Shipments.Add(shipment);
        }

        _db.CartItems.RemoveRange(cart.CartItems);
        _db.SaveChanges();

        TempData["SuccessMessage"] = $"Order #ORD-{order.OrderID:D5} placed successfully!";
        return RedirectToPage("/Orders");
    }

    private void PopulateOrderSummary(Cart cart)
    {
        decimal subtotal = 0;
        if (cart.CartItems != null)
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

        ViewData["CartItems"] = cart.CartItems?.ToList() ?? new List<CartItem>();
        ViewData["Subtotal"] = subtotal;
        ViewData["Tax"] = tax;
        ViewData["Total"] = total;
    }
}
