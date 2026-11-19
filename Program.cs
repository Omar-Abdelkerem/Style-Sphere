using Microsoft.EntityFrameworkCore;
using StyleSphere.Data;

var builder = WebApplication.CreateBuilder(args);

// Step 1: Add Razor Pages support
builder.Services.AddRazorPages();

// Step 2: Add Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Step 3: Configure Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Detect SQLite vs SQL Server:
// - Uses SQL Server if connection string explicitly specifies 'Server=' or ';Database='
// - Defaults to SQLite ('Data Source=StyleSphere.db') for zero-configuration local runs and containers
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

// Step 4: Ensure Database and Seed Initial Data
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
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Step 5: Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
