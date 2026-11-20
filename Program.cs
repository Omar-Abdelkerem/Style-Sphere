using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;
using StyleSphere.Services;

var builder = WebApplication.CreateBuilder(args);

// Boot-time production guard against weak/placeholder secret environment variables
if (builder.Environment.IsProduction())
{
    var adminEnvPwd = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD");
    var weakPlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "password", "changeme", "secret", "placeholder", "12345", "admin"
    };
    if (adminEnvPwd != null && (adminEnvPwd.Length < 6 || weakPlaceholders.Contains(adminEnvPwd)))
    {
        throw new InvalidOperationException("Refusing to start in Production: ADMIN_INITIAL_PASSWORD is too short or uses a known weak placeholder.");
    }
}

// Step 1: Add Razor Pages with Global Authentication & Authorization PageFilter
builder.Services.AddScoped<AuthPageFilter>();
builder.Services.AddRazorPages()
    .AddMvcOptions(options =>
    {
        options.Filters.AddService<AuthPageFilter>();
    });

// Step 2: Harden Antiforgery (CSRF) Cookies
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "__StyleSphere.CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.HeaderName = "X-CSRF-TOKEN";
});

// Step 3: Add Session Support with Hardened Cookie Policy
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "__StyleSphere.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Step 4: Configure Per-IP Rate Limiting for Auth & Public Form Endpoints
int authPermits = builder.Configuration.GetValue<int>("Security:AuthRateLimitPermitsPerMinute", 10);
int formPermits = builder.Configuration.GetValue<int>("Security:FormRateLimitPermitsPerMinute", 20);

builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    limiterOptions.AddPolicy("auth", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermits,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });

    limiterOptions.AddPolicy("forms", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = formPermits,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

// Step 5: Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

bool isSqlServer = !string.IsNullOrWhiteSpace(connectionString) &&
    (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
     connectionString.Contains(";Database=", StringComparison.OrdinalIgnoreCase));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isSqlServer)
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        });
    }
    else
    {
        var sqliteConnection = !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase)
            ? connectionString
            : "Data Source=StyleSphere.db";
        options.UseSqlite(sqliteConnection);
    }
    options.EnableSensitiveDataLogging(false);
});

var app = builder.Build();

// Step 6: Ensure Database Schema and Seed Initial Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Step 7: Security Headers, Source-Map Blocking, NUL-Byte Validation, & Cross-Origin Check
app.Use(async (context, next) =>
{
    // Apply Security Headers
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data:; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    var path = context.Request.Path.Value ?? string.Empty;
    var query = context.Request.QueryString.Value ?? string.Empty;

    // Never serve source map (.map) files publicly
    if (path.EndsWith(".map", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    // Reject requests containing NUL bytes in path or query string
    if (path.Contains('\0') || query.Contains("%00", StringComparison.OrdinalIgnoreCase) || query.Contains('\0'))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Malformed request: NUL bytes are not allowed.");
        return;
    }

    // For state-changing methods, enforce same-origin Origin header when present + check form fields for NUL bytes
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method))
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri) ||
                !string.Equals(originUri.Host, context.Request.Host.Host, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Cross-origin state-changing request rejected.");
                return;
            }
        }

        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = await context.Request.ReadFormAsync();
                foreach (var kvp in form)
                {
                    if (kvp.Key.Contains('\0') || kvp.Value.Any(v => v != null && v.Contains('\0')))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Malformed form input: NUL bytes are not allowed.");
                        return;
                    }
                }
            }
            catch (InvalidDataException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Malformed form payload.");
                return;
            }
        }
    }

    await next();
});

// Step 8: Exception & Status Code Handling (Never expose raw stack traces)
app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/NotFound", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Serve static files with cache headers for public assets
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800";
    }
});

app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthorization();

// Dynamic Sitemap XML endpoint reflecting live database content
app.MapGet("/sitemap.xml", async (HttpContext httpContext, AppDbContext db) =>
{
    var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
    var productIds = await db.Products.AsNoTracking().OrderBy(p => p.ProductID).Select(p => p.ProductID).ToListAsync();
    var categoryIds = await db.Categories.AsNoTracking().OrderBy(c => c.CategoryID).Select(c => c.CategoryID).ToListAsync();

    var sb = new StringBuilder();
    sb.AppendLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
    sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

    var publicRoutes = new[] { "/", "/Products", "/Policies", "/login", "/Register" };
    foreach (var route in publicRoutes)
    {
        sb.AppendLine($"  <url><loc>{baseUrl}{route}</loc><changefreq>weekly</changefreq></url>");
    }

    foreach (var catId in categoryIds)
    {
        sb.AppendLine($"  <url><loc>{baseUrl}/Products?categoryId={catId}</loc><changefreq>weekly</changefreq></url>");
    }

    foreach (var prodId in productIds)
    {
        sb.AppendLine($"  <url><loc>{baseUrl}/details/{prodId}</loc><changefreq>daily</changefreq></url>");
    }

    sb.AppendLine("</urlset>");

    httpContext.Response.ContentType = "application/xml; charset=utf-8";
    httpContext.Response.Headers["Cache-Control"] = "public, max-age=3600";
    await httpContext.Response.WriteAsync(sb.ToString());
});

app.MapRazorPages();

app.Run();

public partial class Program { }
