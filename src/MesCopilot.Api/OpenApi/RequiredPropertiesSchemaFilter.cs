using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MesCopilot.Api.OpenApi;

public sealed class RequiredPropertiesSchemaFilter : ISchemaFilter
{
    private static readonly NullabilityInfoContext Nullability = new();

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties.Count == 0)
        {
            return;
        }

        Dictionary<string, PropertyInfo> properties = context.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(GetJsonName, StringComparer.OrdinalIgnoreCase);
        foreach (string propertyName in schema.Properties.Keys)
        {
            if (properties.TryGetValue(propertyName, out PropertyInfo? property) && IsRequired(property))
            {
                schema.Required.Add(propertyName);
            }
        }
    }

    private static bool IsRequired(PropertyInfo property)
    {
        if (property.GetCustomAttribute<RequiredAttribute>() is not null)
        {
            return true;
        }

        Type propertyType = property.PropertyType;
        if (propertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(propertyType) is null;
        }

        return Nullability.Create(property).ReadState == NullabilityState.NotNull;
    }

    private static string GetJsonName(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
        JsonNamingPolicy.CamelCase.ConvertName(property.Name);
}
