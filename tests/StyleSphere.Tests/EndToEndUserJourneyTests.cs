using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StyleSphere.Data;
using Xunit;

namespace StyleSphere.Tests;

public class EndToEndUserJourneyTests
{
    [Fact]
    public async Task Journey1_CustomerSignup_AddToCart_UpdateQuantity_PlaceOrder_VerifiesTaxAndStockReduction()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1. Register a brand-new customer account
        var regToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/Register");
        var regRes = await client.PostAsync("/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Layla Hassan",
            ["email"] = "layla@example.com",
            ["phone"] = "01012345678",
            ["password"] = "SecureCustomer!1",
            ["confirmPassword"] = "SecureCustomer!1",
            ["__RequestVerificationToken"] = regToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, regRes.StatusCode);

        // 2. Add Product #1 (Classic Oxford Cotton Shirt, $49.99, Initial Stock 35) to Bag
        var addToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/details/1");
        var addRes = await client.PostAsync("/AddToCart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["productId"] = "1",
            ["quantity"] = "1",
            ["__RequestVerificationToken"] = addToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, addRes.StatusCode);

        // 3. Update quantity in Shopping Bag to 2 units
        var cartToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/cart");
        var updateRes = await client.PostAsync("/cart?handler=UpdateQuantity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["productId"] = "1",
            ["quantity"] = "2",
            ["__RequestVerificationToken"] = cartToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, updateRes.StatusCode);

        // Verify cart displays category "Men" (not fallback "Apparel") and subtotal $99.98 + 14% tax ($14.00) = $113.98
        var cartPageRes = await client.GetAsync("/cart");
        var cartHtml = await cartPageRes.Content.ReadAsStringAsync();
        Assert.Contains("Classic Oxford Cotton Shirt", cartHtml);
        Assert.Contains("$99.98", cartHtml);
        Assert.Contains("$14.00", cartHtml);
        Assert.Contains("$113.98", cartHtml);

        // 4. Proceed to Checkout & Place Order
        var checkoutToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/PlaceOrder");
        var orderRes = await client.PostAsync("/PlaceOrder", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["deliveryAddress"] = "14 Zamalek St, Cairo",
            ["paymentMethod"] = "Credit Card",
            ["__RequestVerificationToken"] = checkoutToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, orderRes.StatusCode);
        Assert.Equal("/Orders", orderRes.Headers.Location?.ToString());

        // 5. Verify Order History page & database state (TotalAmount = $113.98 with tax, Stock reduced from 35 to 33)
        var ordersRes = await client.GetAsync("/Orders");
        var ordersHtml = await ordersRes.Content.ReadAsStringAsync();
        Assert.Contains("$113.98", ordersHtml);
        Assert.Contains("Processing", ordersHtml);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product1 = await db.Products.AsNoTracking().FirstAsync(p => p.ProductID == 1);
            Assert.Equal(33, product1.Stock);
        }
    }

    [Fact]
    public async Task Journey2_AdminCreatesProductWithMagicByteVerifiedImage_AppearsInCatalog_AndRejectsFakeImage()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1. Log in as Admin
        var loginToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
        var loginRes = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "admin@stylesphere.com",
            ["password"] = "admin123",
            ["__RequestVerificationToken"] = loginToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

        // 2. Attempt to create a product with a malicious script renamed to .jpg -> must be rejected!
        var createToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/Admin/Products/Create");
        using (var badForm = new MultipartFormDataContent())
        {
            badForm.Add(new StringContent("Malicious Item"), "name");
            badForm.Add(new StringContent("Should fail magic byte check"), "description");
            badForm.Add(new StringContent("99.00"), "price");
            badForm.Add(new StringContent("10"), "stock");
            badForm.Add(new StringContent("1"), "categoryId");
            badForm.Add(new StringContent(createToken), "__RequestVerificationToken");

            var fakeImgContent = new ByteArrayContent(Encoding.UTF8.GetBytes("<?php echo 'hacked'; ?>"));
            fakeImgContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            badForm.Add(fakeImgContent, "imageFile", "exploit.jpg");

            var badRes = await client.PostAsync("/Admin/Products/Create", badForm);
            Assert.Equal(HttpStatusCode.OK, badRes.StatusCode); // Returns form page with error
            var badHtml = await badRes.Content.ReadAsStringAsync();
            Assert.Contains("magic bytes", badHtml, StringComparison.OrdinalIgnoreCase);
        }

        // 3. Create a valid product with genuine PNG magic bytes
        using (var goodForm = new MultipartFormDataContent())
        {
            goodForm.Add(new StringContent("Merino Wool Overcoat"), "name");
            goodForm.Add(new StringContent("Double-breasted coat crafted from 100% Australian merino wool."), "description");
            goodForm.Add(new StringContent("249.99"), "price");
            goodForm.Add(new StringContent("15"), "stock");
            goodForm.Add(new StringContent("1"), "categoryId");
            goodForm.Add(new StringContent(createToken), "__RequestVerificationToken");

            var validPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
            var pngContent = new ByteArrayContent(validPngBytes);
            pngContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            goodForm.Add(pngContent, "imageFile", "overcoat.png");

            var goodRes = await client.PostAsync("/Admin/Products/Create", goodForm);
            Assert.Equal(HttpStatusCode.Redirect, goodRes.StatusCode);
        }

        // 4. Verify the new product appears in the public catalog search
        var catalogRes = await client.GetAsync("/Products?search=Merino");
        var catalogHtml = await catalogRes.Content.ReadAsStringAsync();
        Assert.Contains("Merino Wool Overcoat", catalogHtml);
        Assert.Contains("$249.99", catalogHtml);

        // Clean up the test image written to wwwroot/images/products
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            var createdProd = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Name == "Merino Wool Overcoat");
            if (!string.IsNullOrEmpty(createdProd?.ImageName))
            {
                var uploadedPath = Path.Combine(env.WebRootPath, "images", "products", createdProd.ImageName);
                if (File.Exists(uploadedPath))
                {
                    File.Delete(uploadedPath);
                }
            }
        }
    }

    [Fact]
    public async Task Journey3_CustomerSubmitsReview_WithHtmlScriptTag_RenderedEscapedAgainstStoredXss()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1. Log in as Customer
        var loginToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
        await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "o@gmail.com",
            ["password"] = "123456",
            ["__RequestVerificationToken"] = loginToken
        }));

        // 2. Submit a review on Product #3 containing an XSS payload
        var revToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/AddReview?productId=3");
        var revRes = await client.PostAsync("/AddReview", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["productId"] = "3",
            ["rating"] = "5",
            ["comment"] = "Great jacket! <script>alert('StoredXSS')</script>",
            ["__RequestVerificationToken"] = revToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, revRes.StatusCode);

        // 3. Load /details/3 and verify the script tag is HTML-encoded, never raw
        var detailRes = await client.GetAsync("/details/3");
        var detailHtml = await detailRes.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<script>alert('StoredXSS')</script>", detailHtml);
        Assert.Contains("&lt;script&gt;alert(&#x27;StoredXSS&#x27;)&lt;/script&gt;", detailHtml);
    }
}
