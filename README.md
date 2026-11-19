# StyleSphere 🛍️✨

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Razor%20Pages-2C8EBB)](https://learn.microsoft.com/aspnet/core)
[![EF Core](https://img.shields.io/badge/Entity%20Framework-Core%208-512BD4)](https://learn.microsoft.com/ef/core)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**StyleSphere** is an editorial-grade fashion e-commerce web application built on **ASP.NET Core 8 Razor Pages** and **Entity Framework Core**. Featuring a clean, high-fashion aesthetic (inspired by COS, SSENSE, and Zara), it delivers a seamless end-to-end shopping experience for customers and a full-featured management dashboard for store administrators.

Designed for instant zero-configuration local runs, Docker containers, and turnkey cloud deployments.

---

## 🌟 Key Features

### 🛍️ Customer Experience
- **Editorial Fashion Catalog**: Curated product categories across Men, Women, Footwear, and Accessories with real high-resolution photography.
- **Dynamic Filtering & Live Search**: Instant category pills and client-side keyword search across titles and descriptions.
- **Product Showcase**: Detailed views with stock status, pricing, sizing details, and verified customer reviews.
- **Interactive Shopping Bag**: Session-backed shopping cart with inline quantity steppers and real-time total calculations.
- **Order Placement & Tracking**: Streamlined checkout pipeline and customer order history.
- **Customer Authentication**: Secure session authentication, user profile management, password resets, and 1-click demo logins.

### ⚙️ Admin Control Center
- **Product Management**: Full CRUD operations for products with inventory tracking and image association.
- **Category Control**: Organize fashion lines into custom categories.
- **Order Tracking**: Inspect customer orders, line items, and payment/fulfillment states.
- **User Administration**: Manage system users and role privileges (`Admin` / `Customer`).
- **Reports & Analytics**: Quick overview of inventory levels and store performance.

### 🚀 Architecture & Portability
- **Zero-Config Database**: Auto-initializes and seeds SQLite (`StyleSphere.db`) on startup—no SQL Server installation required.
- **Dual Database Engine**: Seamlessly switch between SQLite and Microsoft SQL Server by updating the connection string.
- **Production-Ready Docker**: Multi-stage `Dockerfile` and `docker-compose.yml` for 1-command containerization.

---

## 🛠️ Tech Stack

- **Framework**: [.NET 8 SDK](https://dotnet.microsoft.com/)
- **Web Layer**: ASP.NET Core Razor Pages
- **ORM**: Entity Framework Core 8.0 (Sqlite & SqlServer providers)
- **Database**: SQLite (default / cross-platform) or Microsoft SQL Server / LocalDB
- **UI & Styling**: HTML5, CSS3, Bootstrap 5, Bootstrap Icons, Google Fonts (`Plus Jakarta Sans` & `Outfit`)
- **Containerization**: Docker & Docker Compose

---

## 🚀 Quick Start (Local Development)

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher (`dotnet --version`)

### 1. Clone the Repository
```bash
git clone https://github.com/Omar-Abdelkerem/Style-Sphere.git
cd Style-Sphere
```

### 2. Run the Application
```bash
dotnet run
```

*(Or use hot reload during development:)*
```bash
dotnet watch
```

### 3. Open in Browser
Navigate to:
```
http://localhost:5000  (or https://localhost:5001)
```

The database (`StyleSphere.db`) is automatically created and pre-seeded with all categories, products, customer reviews, and demo users on first launch!

---

## 🔑 Demo Credentials

For quick testing without typing, 1-click demo buttons are provided on the Login page:

| Role | Email | Password |
| :--- | :--- | :--- |
| **Admin** | `admin@stylesphere.com` | `admin123` |
| **Customer** | `o@gmail.com` | `123456` |

---

## 🐳 Running with Docker

StyleSphere is fully dockerized for instant deployment on any host.

### Using Docker Compose (Recommended)
```bash
docker compose up --build
```
Access the application at: **`http://localhost:5000`**

### Using Docker CLI Directly
```bash
# Build image
docker build -t stylesphere .

# Run container
docker run -d -p 5000:8080 --name stylesphere-app stylesphere
```

---

## 📁 Project Structure

```text
Style-Sphere/
├── Data/
│   ├── AppDbContext.cs          # EF Core context with entity configurations
│   └── DbInitializer.cs         # Automatic database schema and catalog seeder
├── Models/                      # Domain entities (Product, Category, Order, Cart, User, etc.)
├── Pages/                       # Razor Pages UI
│   ├── Admin/                   # Admin management views (Products, Categories, Users, Reports)
│   ├── Shared/                  # Master layouts (_Layout, _CustomerLayout)
│   ├── Index.cshtml             # Homepage with editorial hero & catalog highlights
│   ├── Products.cshtml          # Filterable catalog & search
│   ├── details.cshtml           # Product details & reviews
│   ├── cart.cshtml              # Shopping cart & line items
│   ├── PlaceOrder.cshtml        # Checkout & order submission
│   ├── Orders.cshtml            # Order history tracking
│   ├── login.cshtml             # Authentication with 1-click demo logins
│   └── Register.cshtml          # New account registration
├── Properties/                  # Launch profiles & runtime settings
├── wwwroot/                     # Static assets
│   ├── css/                     # Custom monochrome editorial styling
│   ├── images/products/         # High-resolution fashion photography
│   └── js/                      # Frontend behaviors
├── appsettings.json             # Configuration & database connection strings
├── appsettings.Development.json # Development-specific settings
├── Dockerfile                   # Multi-stage production container build
├── docker-compose.yml           # Compose specification
├── Program.cs                   # Application entrypoint & middleware pipeline
├── StyleSphere.csproj           # Project configuration & NuGet dependencies
└── StyleSphere.sln              # Visual Studio solution file
```

---

## ⚙️ Database Configuration

StyleSphere dynamically adapts to your configuration in `appsettings.json`:

### Option A: SQLite (Default & Recommended for Containers)
Zero-configuration local file database. Works across Windows, macOS, Linux, and Docker:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=StyleSphere.db"
  }
}
```

### Option B: Microsoft SQL Server
To connect to SQL Server or Azure SQL Database, provide a standard SQL connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=StyleSphere;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  }
}
```
*`Program.cs` automatically detects SQL Server if `Server=` is present in the connection string and applies retry policies accordingly.*

---

## ☁️ Deployment Guide

### Deploying to Cloud Platforms (Render, Railway, Fly.io, Azure)

#### Deploy with Docker (Render / Railway / Fly.io):
1. Connect your GitHub repository: `https://github.com/Omar-Abdelkerem/Style-Sphere`
2. Select **Docker** as the deployment runtime.
3. The platform will automatically detect `Dockerfile` and expose port `8080`.
4. Your application will launch with SQLite pre-seeded and ready to use!

#### Deploy to Azure App Service:
1. Create a **Linux App Service** with .NET 8 runtime.
2. Configure GitHub Actions continuous deployment from the repository `main` branch.
3. (Optional) Set `ConnectionStrings__DefaultConnection` in Azure Configuration if connecting to Azure SQL.

---

## 📝 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
