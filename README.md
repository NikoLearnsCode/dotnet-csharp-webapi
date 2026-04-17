**A .NET REST API demonstrating a clean layered architecture, built with C# and Entity Framework Core to handle e-commerce logic and data persistence.**

## High-level structure

- `Controllers`: HTTP endpoints (Auth, Products, Categories, Cart, Orders).
- `Services`: business logic and orchestration.
- `Data`: EF Core `DbContext`, entities, seed data, migrations.
- `DTOs` and `Mapping`: API contracts and AutoMapper profiles.

## Local setup

1. Configure `ConnectionStrings:DefaultConnection` in `appsettings.Development.json` to a local SQL Server instance.
2. Seed and run the API: `dotnet ef database update && dotnet run`.
3. Test the API: Open Swagger UI at http://localhost:5252/swagger or import the OpenAPI URL into your preferred API client (e.g., Insomnia): http://localhost:5252/swagger/v1/swagger.json.

The seed includes a local-only admin for `POST /api/auth/login`: username `admin`, password `Admin123` (role `Admin`), for trying protected endpoints in development.
