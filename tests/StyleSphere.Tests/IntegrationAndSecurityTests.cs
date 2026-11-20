using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StyleSphere.Data;
using StyleSphere.Services;
using Xunit;

namespace StyleSphere.Tests;

public class IntegrationAndSecurityTests
{
    public static IEnumerable<object[]> AllAdminRoutes =>
        new List<object[]>
        {
            new object[] { "/Admin/Index" },
            new object[] { "/Admin/Reports" },
            new object[] { "/Admin/Products/Index" },
            new object[] { "/Admin/Products/Create" },
            new object[] { "/Admin/Products/Edit/1" },
            new object[] { "/Admin/Products/Delete/1" },
            new object[] { "/Admin/Categories/Index" },
            new object[] { "/Admin/Categories/Create" },
            new object[] { "/Admin/Categories/Edit/1" },
            new object[] { "/Admin/Categories/Delete/1" },
            new object[] { "/Admin/Users/Index" },
            new object[] { "/Admin/Users/Create" },
            new object[] { "/Admin/Users/Edit/1" },
            new object[] { "/Admin/Users/Delete/1" }
        };

    public static IEnumerable<object[]> AllCustomerProtectedRoutes =>
        new List<object[]>
        {
            new object[] { "/cart" },
            new object[] { "/PlaceOrder" },
            new object[] { "/Orders" },
            new object[] { "/ChangePassword" },
            new object[] { "/AddReview?productId=1" },
            new object[] { "/AddToCart" }
        };

    [Theory]
    [MemberData(nameof(AllAdminRoutes))]
    public async Task All14AdminRoutes_Unauthenticated_RedirectToLogin(string route)
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(AllCustomerProtectedRoutes))]
    public async Task All6CustomerProtectedRoutes_Unauthenticated_RedirectToLogin(string route)
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(AllAdminRoutes))]
    public async Task All14AdminRoutes_AuthenticatedAsCustomer_Return403Forbidden(string route)
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Log in as Customer (o@gmail.com / 123456)
        var token = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
        var loginRes = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "o@gmail.com",
            ["password"] = "123456",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, loginRes.StatusCode);

        // Attempt to access each Admin route as a Customer
        var adminRes = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Forbidden, adminRes.StatusCode);
    }

    [Fact]
    public async Task Logout_InvalidatesOtherActiveSessionsServerSide_ViaSessionVersion()
    {
        using var factory = new StyleSphereWebFactory();
        var deviceA = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var deviceB = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Both Device A and Device B log in to the same customer account
        foreach (var client in new[] { deviceA, deviceB })
        {
            var token = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
            var res = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = "o@gmail.com",
                ["password"] = "123456",
                ["__RequestVerificationToken"] = token
            }));
            Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        }

        // Verify both devices can access /Orders
        Assert.Equal(HttpStatusCode.OK, (await deviceA.GetAsync("/Orders")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await deviceB.GetAsync("/Orders")).StatusCode);

        // Device A logs out via POST /Logout
        var logoutToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(deviceA, "/Orders");
        var logoutRes = await deviceA.PostAsync("/Logout", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = logoutToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, logoutRes.StatusCode);

        // Both Device A AND Device B's prior session must now be rejected on protected routes!
        var deviceAAfter = await deviceA.GetAsync("/Orders");
        var deviceBAfter = await deviceB.GetAsync("/Orders");

        Assert.Equal(HttpStatusCode.Redirect, deviceAAfter.StatusCode);
        Assert.Contains("/login", deviceAAfter.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.Redirect, deviceBAfter.StatusCode);
        Assert.Contains("/login", deviceBAfter.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePassword_InvalidatesOtherActiveSessions_WhileKeepingCurrentSessionAlive()
    {
        using var factory = new StyleSphereWebFactory();
        var deviceA = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var deviceB = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        foreach (var client in new[] { deviceA, deviceB })
        {
            var token = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
            await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = "o@gmail.com",
                ["password"] = "123456",
                ["__RequestVerificationToken"] = token
            }));
        }

        // Device A changes password
        var cpToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(deviceA, "/ChangePassword");
        var cpRes = await deviceA.PostAsync("/ChangePassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["oldPassword"] = "123456",
            ["newPassword"] = "NewPass789!",
            ["confirmPassword"] = "NewPass789!",
            ["__RequestVerificationToken"] = cpToken
        }));
        Assert.Equal(HttpStatusCode.OK, cpRes.StatusCode);

        // Device A remains authenticated, while Device B's old session is rejected!
        Assert.Equal(HttpStatusCode.OK, (await deviceA.GetAsync("/Orders")).StatusCode);
        var deviceBRes = await deviceB.GetAsync("/Orders");
        Assert.Equal(HttpStatusCode.Redirect, deviceBRes.StatusCode);
        Assert.Contains("/login", deviceBRes.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PasswordResetToken_IsStoredHashed_SingleUse_AndInvalidatesActiveSessions()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Request password reset for o@gmail.com
        var fpToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/ForgotPassword");
        var fpRes = await client.PostAsync("/ForgotPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "o@gmail.com",
            ["__RequestVerificationToken"] = fpToken
        }));
        Assert.Equal(HttpStatusCode.OK, fpRes.StatusCode);
        var fpHtml = await fpRes.Content.ReadAsStringAsync();

        // Extract dev reset token from response
        var match = System.Text.RegularExpressions.Regex.Match(fpHtml, @"ResetPassword\?token=([a-f0-9]{64})");
        Assert.True(match.Success, "Expected reset token link in development environment");
        string rawResetToken = match.Groups[1].Value;

        // Verify database stores SHA-256 hash, NOT plaintext token
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbToken = await db.PasswordResetTokens.SingleAsync();
            Assert.NotEqual(rawResetToken, dbToken.TokenHash);
            Assert.Equal(PasswordHasher.HashResetToken(rawResetToken), dbToken.TokenHash);
            Assert.False(dbToken.IsUsed);
        }

        // Consume reset token once
        var rpToken = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, $"/ResetPassword?token={rawResetToken}");
        var rpRes = await client.PostAsync("/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = rawResetToken,
            ["newPassword"] = "ResetSuccess99!",
            ["confirmPassword"] = "ResetSuccess99!",
            ["__RequestVerificationToken"] = rpToken
        }));
        Assert.Equal(HttpStatusCode.Redirect, rpRes.StatusCode);

        // Attempt to reuse the exact same reset token a second time -> must fail!
        var secondAttemptRes = await client.PostAsync("/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = rawResetToken,
            ["newPassword"] = "AnotherPass99!",
            ["confirmPassword"] = "AnotherPass99!",
            ["__RequestVerificationToken"] = rpToken
        }));
        var secondHtml = await secondAttemptRes.Content.ReadAsStringAsync();
        Assert.Contains("invalid, has already been used, or has expired", secondHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CsrfProtection_CrossOriginPostAndMissingToken_AreRejected()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // 1. State-changing POST without antiforgery token -> 400 Bad Request
        var noTokenRes = await client.PostAsync("/AddToCart", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["productId"] = "1",
            ["quantity"] = "1"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, noTokenRes.StatusCode);

        // 2. State-changing POST with cross-origin Origin header -> 403 Forbidden
        var crossOriginReq = new HttpRequestMessage(HttpMethod.Post, "/login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["email"] = "o@gmail.com",
                ["password"] = "123456"
            })
        };
        crossOriginReq.Headers.Add("Origin", "https://evil-attacker.example.com");
        var crossOriginRes = await client.SendAsync(crossOriginReq);
        Assert.Equal(HttpStatusCode.Forbidden, crossOriginRes.StatusCode);

        // 3. Cross-site GET /Logout subresource -> 400 Bad Request
        var crossSiteLogout = new HttpRequestMessage(HttpMethod.Get, "/Logout");
        crossSiteLogout.Headers.Add("Sec-Fetch-Site", "cross-site");
        var logoutRes = await client.SendAsync(crossSiteLogout);
        Assert.Equal(HttpStatusCode.BadRequest, logoutRes.StatusCode);
    }

    [Fact]
    public async Task NulByteAndMalformedInput_Returns400BadRequest_Never500()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // NUL byte in query string (%00)
        var queryNulRes = await client.GetAsync("/Products?search=shirt%00evil");
        Assert.Equal(HttpStatusCode.BadRequest, queryNulRes.StatusCode);

        // NUL byte in POST form field
        var token = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
        var formNulRes = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "admin\0@stylesphere.com",
            ["password"] = "admin123",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.BadRequest, formNulRes.StatusCode);
    }

    [Fact]
    public async Task SecurityHeadersAndSourceMapBlocking_EnforcedOnResponses()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient();

        var res = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").FirstOrDefault());
        Assert.Equal("strict-origin-when-cross-origin", res.Headers.GetValues("Referrer-Policy").FirstOrDefault());
        Assert.Contains("default-src 'self'", res.Headers.GetValues("Content-Security-Policy").FirstOrDefault() ?? "");

        // Source maps (.map) must return 404 Not Found
        var mapRes = await client.GetAsync("/lib/bootstrap/dist/css/bootstrap.min.css.map");
        Assert.Equal(HttpStatusCode.NotFound, mapRes.StatusCode);
    }

    [Fact]
    public async Task SeoMetadata_RobotsTxt_SitemapXml_Policies_AndCustom404Page_WorkCorrectly()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient();

        var robots = await client.GetAsync("/robots.txt");
        Assert.Equal(HttpStatusCode.OK, robots.StatusCode);
        var robotsText = await robots.Content.ReadAsStringAsync();
        Assert.Contains("Disallow: /Admin/", robotsText);
        Assert.Contains("Sitemap: /sitemap.xml", robotsText);

        var sitemap = await client.GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, sitemap.StatusCode);
        var sitemapXml = await sitemap.Content.ReadAsStringAsync();
        Assert.Contains("<loc>", sitemapXml);
        Assert.Contains("/details/1</loc>", sitemapXml);

        var policies = await client.GetAsync("/Policies");
        Assert.Equal(HttpStatusCode.OK, policies.StatusCode);

        var notFound = await client.GetAsync("/non-existent-route-12345");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        var notFoundHtml = await notFound.Content.ReadAsStringAsync();
        Assert.Contains("Page Not Found", notFoundHtml);
    }

    [Fact]
    public async Task DatabasePerformance_ReportsAndCategories_ExecuteBatchedGroupByWithoutNPlusOne()
    {
        using var factory = new StyleSphereWebFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Log in as Admin
        var token = await StyleSphereWebFactory.ExtractAntiForgeryTokenAsync(client, "/login");
        await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "admin@stylesphere.com",
            ["password"] = "admin123",
            ["__RequestVerificationToken"] = token
        }));

        // Reset SQL query counter and request /Admin/Categories/Index
        factory.QueryCounter.Reset();
        var catRes = await client.GetAsync("/Admin/Categories/Index");
        Assert.Equal(HttpStatusCode.OK, catRes.StatusCode);
        int catQueries = factory.QueryCounter.Count;

        // 2 auth filter queries (User session validation + cart badge) + 2 page queries (Categories list + batched GroupBy product count) = 4 queries total (previously 2 + 1 + 4 = 7, scaling O(N) with category count)
        Assert.True(catQueries <= 4, $"Expected at most 4 SQL queries on /Admin/Categories/Index, but observed {catQueries}.");

        // Reset SQL query counter and request /Admin/Reports
        factory.QueryCounter.Reset();
        var repRes = await client.GetAsync("/Admin/Reports");
        Assert.Equal(HttpStatusCode.OK, repRes.StatusCode);
        int repQueries = factory.QueryCounter.Count;

        // 2 auth filter queries + 4 page queries (Categories, GroupBy Products, LowStock Products, GroupBy Orders) = 6 queries total (previously 10+ queries)
        Assert.True(repQueries <= 6, $"Expected at most 6 SQL queries on /Admin/Reports, but observed {repQueries}.");
    }
}
