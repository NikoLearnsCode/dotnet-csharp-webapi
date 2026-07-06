using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebApi;

/// <summary>
/// Marks operations that use [Authorize] in the OpenAPI document so Swagger UI shows the
/// lock and documents 401/403. References the CookieAuth scheme defined in Program.cs:
/// auth rides on the httpOnly jwt cookie from login, never on a manually entered token.
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // [AllowAnonymous] overrides [Authorize] no matter which level either sits on,
        // so it must win here too or the doc would show locks on open endpoints.
        if (
            context.MethodInfo.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any()
            || (
                context
                    .MethodInfo.DeclaringType?.GetCustomAttributes<AllowAnonymousAttribute>(
                        inherit: true
                    )
                    .Any() ?? false
            )
        )
            return;

        var actionAttrs = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
        var controllerAttrs =
            context.MethodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            ?? Enumerable.Empty<AuthorizeAttribute>();

        if (!actionAttrs.Any() && !controllerAttrs.Any())
            return;

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "CookieAuth",
            },
        };

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new() { [scheme] = Array.Empty<string>() },
        };
    }
}
