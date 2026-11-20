# StyleSphere

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Razor%20Pages-2C8EBB)](https://learn.microsoft.com/aspnet/core)
[![EF Core](https://img.shields.io/badge/Entity%20Framework-Core%208.0.34-512BD4)](https://learn.microsoft.com/ef/core)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Tests](https://img.shields.io/badge/Tests-45%20Passed-success)](tests/StyleSphere.Tests)

**StyleSphere** is a production-hardened fashion e-commerce web application built on **ASP.NET Core 8 Razor Pages** and **Entity Framework Core**. Featuring a monochrome editorial aesthetic inspired by COS, SSENSE, and Zara, it provides an end-to-end customer shopping journey and an administrator management console.

---

## Key Features

### Storefront & Customer Experience
- **Editorial Fashion Catalog**: Curated collections across Men, Women, Footwear, and Accessories with high-resolution product photography and SQL-paginated browsing.
- **Dynamic Filtering & Search**: Instant category filtering and keyword search across product titles and descriptions.
- **Product Showcase & Verified Reviews**: Detailed specification views, live inventory status, and customer review submissions protected against stored XSS.
- **Shopping Bag & Checkout**: Session-authenticated bag with live header badge counter, inline quantity controls, itemized tax (14% VAT), and delivery destination capture.
- **Client Care & Policies**: Dedicated policies page (`/Policies`) covering shipping, returns, privacy, and terms of service, plus dynamic `/sitemap.xml` and `/robots.txt`.

### Security & Production Hardening
- **Cryptographic Password Hashing**: Salted **PBKDF2-HMAC-SHA512** with **210,000 iterations** and constant-time comparison (`CryptographicOperations.FixedTimeEquals`).
- **Server-Side Session Versioning**: Every account tracks a `SessionVersion` counter in the database. Logging out or changing a password increments `SessionVersion`, immediately invalidating all other active sessions across devices.
- **Server-Side Authorization Filter**: Global `AuthPageFilter` enforces authentication on all customer routes and strict `Admin` role verification (`403 Forbidden`) across all `/Admin/**` routes.
- **Magic-Byte File Upload Validation**: Product image uploads inspect actual file header signatures (JPEG, PNG, GIF, WebP magic bytes), enforce a 5 MB limit, reject SVG/executable extensions, and store files under generated UUID filenames.
- **CSRF, Rate Limiting & Security Headers**:
  - Strict `SameSite` + `HttpOnly` cookies, anti-forgery tokens on all state-changing forms, and same-origin `Origin`/`Sec-Fetch-Site` enforcement.
  - Per-IP fixed-window rate limiting on authentication and public form endpoints (`429 Too Many Requests`).
  - Strict `Content-Security-Policy` (no `'unsafe-inline'` scripts), `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`, `Permissions-Policy`, NUL-byte request filtering, and `.map` source-map blocking.

---

## Tech Stack

- **Runtime**: [.NET 8 SDK](https://dotnet.microsoft.com/)
- **Web Framework**: ASP.NET Core 8 Razor Pages
- **ORM**: Entity Framework Core 8.0.34 (SQLite & SQL Server providers)
- **Database**: SQLite (zero-configuration default) or Microsoft SQL Server
- **Frontend**: HTML5, CSS3 (WCAG AA contrast compliant), Bootstrap 5, Bootstrap Icons
- **Testing**: xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`)
- **Containerization**: Multi-stage `Dockerfile` & `docker-compose.yml`

---

## Quick Start (Local Development)

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher

### 1. Clone & Run
```bash
git clone https://github.com/Omar-Abdelkerem/Style-Sphere.git
cd Style-Sphere
dotnet run --project StyleSphere.csproj
```

Navigate to `http://localhost:5000`. The SQLite database (`StyleSphere.db`) is automatically initialized and seeded on startup.

### 2. Run the Automated Test Suite
```bash
dotnet test
```
Runs 45 automated unit, integration, security, and end-to-end user journey tests against isolated test databases.

---

## Running with Docker

```bash
docker compose up --build
```
Access the application at `http://localhost:5000` (container listens on port `8080`).

---

## Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=StyleSphere.db"
  },
  "StoreSettings": {
    "TaxRate": 0.14,
    "LowStockThreshold": 10,
    "FreeShippingThreshold": 50.00,
    "PageSize": 12
  },
  "Security": {
    "PasswordResetTokenExpiryMinutes": 15,
    "AuthRateLimitPermitsPerMinute": 10,
    "FormRateLimitPermitsPerMinute": 20
  }
}
```
- **Custom Initial Admin Password in Production**: Set the environment variable `ADMIN_INITIAL_PASSWORD` before first launch.
- **Switching to SQL Server**: Set `ConnectionStrings__DefaultConnection` to a SQL Server connection string containing `Server=...`.
