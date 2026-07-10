using MesCopilot.Api.Dtos.Tenants;
using MesCopilot.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MesCopilot.Api.OpenApi;

public sealed class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!RequiresTenant(context))
        {
            return;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TenantResolutionMiddleware.HeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Active tenant selected from the authenticated user's memberships.",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });

        OpenApiSchema problemSchema = context.SchemaGenerator.GenerateSchema(
            typeof(TenantProblemDetails),
            context.SchemaRepository);
        operation.Responses.TryAdd("400", CreateProblemResponse("Tenant selection is required.", problemSchema));
        operation.Responses.TryAdd("403", CreateProblemResponse("Tenant membership is missing or inactive.", problemSchema));
    }

    private static bool RequiresTenant(OperationFilterContext context)
    {
        IList<object> metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
        {
            return false;
        }

        string path = context.ApiDescription.RelativePath ?? string.Empty;
        if (path.Equals("api/auth/me", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!path.StartsWith("api/tenants", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return path.Equals("api/tenants/current", StringComparison.OrdinalIgnoreCase);
    }

    private static OpenApiResponse CreateProblemResponse(string description, OpenApiSchema schema)
    {
        return new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new() { Schema = schema }
            }
        };
    }
}
