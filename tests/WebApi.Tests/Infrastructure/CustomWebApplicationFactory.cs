using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using WebApi.Data;

namespace WebApi.Tests.Infrastructure;

/// <summary>
/// One SQL Server container shared by the whole test run. Starting the container is
/// the expensive part (seconds, plus x64 emulation on Apple Silicon), so it is paid
/// once; isolation instead comes from giving each factory its own database inside
/// the container. LazyTask guarantees a single start even when xUnit runs test
/// classes in parallel. Testcontainers' reaper (Ryuk) removes the container when the
/// test process exits, so there is nothing to clean up manually.
/// </summary>
public static class SqlServerTestContainer
{
    private static readonly Lazy<Task<MsSqlContainer>> Instance = new(async () =>
    {
        // Same image as the local dev container on Apple Silicon
        // it runs via Docker Desktop's Rosetta x86/amd64 emulation.
        var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await container.StartAsync();
        return container;
    });

    public static string ConnectionString =>
        Instance.Value.GetAwaiter().GetResult().GetConnectionString();
}

/// <summary>
/// Boots the real API in-memory for integration tests against a real SQL Server
/// (Testcontainers), but swaps JWT auth for <see cref="TestAuthHandler"/>.
/// Each factory instance - one per test class via IClassFixture - gets its own
/// freshly created and seeded database in the shared container.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"test_{Guid.NewGuid():N}";

    private string ConnectionString =>
        new SqlConnectionStringBuilder(SqlServerTestContainer.ConnectionString)
        {
            InitialCatalog = _databaseName,
        }.ConnectionString;

    /// <summary>
    /// When true (default), JWT auth is replaced with <see cref="TestAuthHandler"/>.
    /// Override to false to exercise the real JwtBearer + cookie flow.
    /// </summary>
    protected virtual bool UseTestAuth => true;

    public const string TestJwtKey =
        "test-signing-key-that-is-definitely-long-enough-for-hmac-sha256-0123456789";

    public const string TestJwtIssuer = "http://localhost";
    public const string TestJwtAudience = "http://localhost";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // appsettings.json ships no Jwt settings (they live in the gitignored
        // appsettings.Development.json). Provide them so JwtOptions validation
        // (ValidateOnStart in Program.cs) passes on machines without that file.
        builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Jwt:Key"] = TestJwtKey,
                        ["Jwt:Issuer"] = TestJwtIssuer,
                        ["Jwt:Audience"] = TestJwtAudience,
                        // The app and the tests use the same provider (SQL Server), so no
                        // service surgery is needed: AddDbContext reads the connection
                        // string lazily when the context is first resolved, at which point
                        // this test value has been merged into the configuration and wins
                        // over appsettings.Development.json.
                        ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                    }
                );
            }
        );

        builder.ConfigureTestServices(services =>
        {
            // Replace JWT cookie auth with the header-driven test scheme. When
            // UseTestAuth is false nothing needs patching: JwtBearer resolves its
            // settings through IOptions<JwtOptions> (see Program.cs) after the
            // in-memory config above has been merged, so both token signing and
            // validation use the test values.
            if (UseTestAuth)
            {
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { }
                    );
            }
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        // Create the per-class database (schema + HasData seed: admin user,
        // categories, products) using the app's real service provider.
        ResetDatabase(host.Services);
        return host;
    }

    /// <summary>
    /// Drops and recreates the database (schema + seed). Useful for tests that
    /// mutate data and need a clean, deterministic starting point.
    /// </summary>
    public void ResetDatabase() => ResetDatabase(Services);

    private static void ResetDatabase(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Drop this class's database so the shared container doesn't accumulate
            // databases over the run. (The container itself is reaped at exit.)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            using var db = new ApplicationDbContext(options);
            db.Database.EnsureDeleted();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Keeps the real JwtBearer authentication (cookie extraction, signature and
/// lifetime validation) so tests can exercise the actual login → cookie → auth flow.
/// </summary>
public class JwtWebApplicationFactory : CustomWebApplicationFactory
{
    protected override bool UseTestAuth => false;
}