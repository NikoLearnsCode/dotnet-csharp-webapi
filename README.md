# ASP.NET Core Web API

REST API built with ASP.NET Core on .NET 10 and EF Core with SQL Server. The project focuses on doing the fundamentals properly: a strictly layered architecture, cookie based JWT authentication with refresh token rotation, and an integration test suite that runs against a real SQL Server.

## API overview

- **Auth** (`/api/auth`): register, login, refresh and logout. Login sets a short lived JWT access token and a rotating refresh token, both as httpOnly cookies. Login and register are rate limited per IP.
- **Products** (`/api/products`): full CRUD with category filtering and two pagination strategies, classic offset paging and cursor based paging for load-more flows. Writes require the Admin role.
- **Categories** (`/api/categories`): a self referencing category tree with branch and leaf nodes. The service layer enforces the rules, children only under branches, products only in leaves, cycle checks on move and delete blocked while children or products exist.
- **Cart** (`/api/cart`): works both logged in and anonymous. Anonymous visitors get a session cookie on first use, and the session cart is merged into the user cart on login.
- **Orders** (`/api/orders`): checkout from the cart, including anonymous checkout where the confirmation is reachable through a confirmation token instead of an account.

## Technical highlights

- **Refresh token rotation with replay detection.** Only the SHA-256 hash of a refresh token is stored. If a revoked token is presented again the raw value must have leaked, so all of the user's active sessions are revoked.
- **Fail fast configuration.** JWT settings are bound to a validated options class at startup, and the app refuses to boot with a missing or too short signing key.
- **Mapping without libraries.** DTO mapping is done with hand written `IQueryable` projection extensions, so projections translate to SQL and entities never leave the API.
- **Tree logic without recursive SQL.** Category tree operations (tree building, subtree lookups, breadcrumbs) load the flat category list once and work in memory.
- **Real integration tests.** The test suite boots the actual app with `WebApplicationFactory` and talks to a real SQL Server started by Testcontainers, no in-memory database stand-ins. Each test class gets its own freshly migrated and seeded database.
- **CI.** GitHub Actions runs the full test suite on every push and pull request to `main`.

## Project structure

Three layers, strictly separated:

- `Controllers`: HTTP endpoints only, no business logic
- `Services`: business logic behind interfaces
- `Data`: EF Core `DbContext`, entities and seed data
- `DTOs` and `Mapping`: API contracts and the LINQ projections

## Getting started

Requires the .NET 10 SDK and Docker. SQL Server runs in a container, and the tests use Testcontainers.

1. Start SQL Server:

   ```bash
   docker run -d --name sqlserver -p 1433:1433 \
     -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<password>" \
     mcr.microsoft.com/mssql/server:2022-latest
   ```

   The password must satisfy the SQL Server complexity policy, otherwise the container exits right after starting.

2. Copy `appsettings.Development.example.json` to `appsettings.Development.json` and fill in the connection string and a `Jwt:Key` of at least 64 characters.

3. Create the database and run the API. Migrations are not checked in, so generate one first on a fresh clone:

   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   dotnet run
   ```

The API listens on http://localhost:5252 with Swagger UI at `/swagger` in Development. Seeding is part of the migrations and includes a development admin (username `admin`, password `Admin123`) for trying protected endpoints.

## Tests

```bash
dotnet test
```

Docker must be running. One SQL Server container is shared by the whole run, while every test class gets its own database with schema and seed data, so tests are isolated without being slow. The tests cover the endpoint groups above plus the full JWT cookie and refresh token flow, including rotation and replay detection.
