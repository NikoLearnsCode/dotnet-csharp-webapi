using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebApi;
using WebApi.Data;
using WebApi.Services;
using WebApi.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApi", Version = "1.0" });
    // Explicit servers prevent clients (Insomnia) from guessing a wrong base URL on import.
    options.AddServer(
        new OpenApiServer
        {
            Url = "http://localhost:5252",
            Description = "Local http (launch profile http)",
        }
    );
    options.AddServer(
        new OpenApiServer
        {
            Url = "https://localhost:7169",
            Description = "Local https (launch profile https)",
        }
    );
    // Documents how auth actually works (httpOnly cookie set by login), rather
    // than a Bearer header no client can ever fill in: the raw JWT never leaves
    // the cookie, so there is nothing to paste into an Authorize dialog.
    options.AddSecurityDefinition(
        "CookieAuth",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT in the httpOnly 'jwt' cookie, set automatically by POST /api/auth/login "
                + "and sent by the browser on every request. Nothing to enter here - just log in; "
                + "the padlock only marks endpoints that require authentication (Admin role for "
                + "product/category writes).",
            Name = AuthCookie.Name,
            In = ParameterLocation.Cookie,
            Type = SecuritySchemeType.ApiKey,
        }
    );
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();

// Unhandled exceptions become RFC 7807 responses instead of empty 500s.
builder.Services.AddProblemDetails();

// Brute-force protection for the auth endpoints (login/register).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        "auth",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                }
            )
    );
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:4173",
                    "https://localhost:5173",
                    "https://localhost:4173"
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    );
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Bound and validated at startup: a missing or too-short key stops the app
// with a clear message (the key lives in the gitignored appsettings.Development.json).
builder
    .Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

// JwtBearer reads its settings through IOptions<JwtOptions> when the options are
// first materialized, after configuration is final - never eagerly from
// builder.Configuration. This keeps the validated JwtOptions as the single
// source for the key and lets test hosts override the Jwt section late
// (WebApplicationFactory merges its in-memory config after Program.cs has run).
builder
    .Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure(
        (JwtBearerOptions options, IOptions<JwtOptions> jwtOptions) =>
        {
            var jwt = jwtOptions.Value;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                // The default 5-minute skew is large next to a 15-minute access token.
                ClockSkew = TimeSpan.FromSeconds(30),
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies[AuthCookie.Name];
                    return Task.CompletedTask;
                },
            };
        }
    );

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the implicit top-level Program class to the test project so
// WebApplicationFactory<Program> can boot the API in integration tests.
public partial class Program { }
