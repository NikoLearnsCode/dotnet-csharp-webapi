using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace dotnet_backend_2;

/// <summary>
/// Marks operations that use [Authorize] in the OpenAPI document so Swagger UI shows the lock and Bearer dialog.
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionAttrs = context.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
        var controllerAttrs = context.MethodInfo.DeclaringType?.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            ?? Enumerable.Empty<AuthorizeAttribute>();

        if (!actionAttrs.Any() && !controllerAttrs.Any())
            return;

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

        var scheme = new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };

        operation.Security = new List<OpenApiSecurityRequirement>
        {
            new()
            {
                [scheme] = Array.Empty<string>()
            }
        };
    }
}
