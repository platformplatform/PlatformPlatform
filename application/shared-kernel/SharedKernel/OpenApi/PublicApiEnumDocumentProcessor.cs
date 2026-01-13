using NJsonSchema;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace PlatformPlatform.SharedKernel.OpenApi;

public sealed class PublicApiEnumDocumentProcessor(Assembly[] assemblies) : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        foreach (var assembly in assemblies)
        {
            var enumsWithJsonStringConverter = assembly.GetTypes()
                .Where(t => t.IsEnum)
                .Where(t => t.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType?.Name == nameof(JsonStringEnumConverter))
                .ToList();

            foreach (var enumType in enumsWithJsonStringConverter)
            {
                if (context.Document.Components.Schemas.ContainsKey(enumType.Name))
                {
                    continue;
                }

                var schema = new JsonSchema
                {
                    Type = JsonObjectType.String
                };

                foreach (var enumValue in Enum.GetNames(enumType))
                {
                    schema.Enumeration.Add(enumValue);
                }

                context.Document.Components.Schemas[enumType.Name] = schema;
            }
        }
    }
}
