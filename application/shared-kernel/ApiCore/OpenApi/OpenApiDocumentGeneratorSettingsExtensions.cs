using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NJsonSchema.Generation;
using NSwag.Generation.AspNetCore;

namespace PlatformPlatform.SharedKernel.ApiCore.OpenApi;

public static class OpenApiDocumentGeneratorSettingsExtensions
{
    /// <summary>
    ///     Configures the schema settings for the OpenAPI document generator
    /// </summary>
    public static void ConfigureSchemaSettings(
        this AspNetCoreOpenApiDocumentGeneratorSettings settings,
        IServiceProvider serviceProvider
    )
    {
        if (settings.SchemaSettings is not SystemTextJsonSchemaGeneratorSettings schemaSettings) return;

        var serializerOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
        schemaSettings.SerializerOptions = new JsonSerializerOptions(serializerOptions);
        schemaSettings.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }
}