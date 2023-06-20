using JetBrains.Annotations;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PlatformPlatform.SharedKernel.ApiCore.Filters;

/// <summary>
///     This class is used to allow Swagger contracts to show the names of the enum values instead of their numeric
///     values.
/// </summary>
[UsedImplicitly]
public class XEnumNamesSchemaFilter : ISchemaFilter
{
    private const string Name = "x-enumNames";

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum) return;
        if (schema.Extensions.ContainsKey(Name)) return;

        var openApiArray = new OpenApiArray();
        var openApiStrings = Enum.GetNames(context.Type).Select(name => new OpenApiString(name));
        openApiArray.AddRange(openApiStrings);
        schema.Extensions.Add(Name, openApiArray);
    }
}