# StyleSphere

Simple e-commerce Razor Pages app for managing products, categories, and orders.

## Prerequisites

- .NET 8 SDK
- A SQL Server instance (or update the connection string in `appsettings.json`)

## Quick start

1. Restore and build:

```bash
dotnet restore
dotnet build
```

2. Update the connection string in `appsettings.json` to point to your database.

3. Run the app:

```bash
dotnet run
```

Notes:

- The project currently uses SQL Server by default. On non-Windows systems you may want to switch to SQLite or run SQL Server in a container and update the connection string accordingly.
- Local development files (build outputs, IDE files, and local DB files) are ignored via `.gitignore`.

## License

This repository has no license specified.
