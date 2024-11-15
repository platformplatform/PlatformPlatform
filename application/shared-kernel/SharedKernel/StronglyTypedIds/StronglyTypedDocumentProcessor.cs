using NJsonSchema;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace PlatformPlatform.SharedKernel.StronglyTypedIds;

public class StronglyTypedDocumentProcessor(Assembly[] assemblies)
    : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        // Find all strongly typed IDs
        foreach (var assembly in assemblies)
        {
            var stronglyTypedIdNames = assembly.GetTypes()
                .Where(t => typeof(IStronglyTypedId).IsAssignableFrom(t))
                .Select(t => new { t.Name, t.GetCustomAttribute<IdPrefixAttribute>()?.Prefix })
                .ToList();

            // Ensure the Swagger UI to correctly display strongly typed IDs as plain text instead of complex objects
            foreach (var stronglyTypedIdName in stronglyTypedIdNames)
            {
                if (!context.Document.Components.Schemas.TryGetValue(stronglyTypedIdName.Name, out var schema)) continue;
                schema.Type = JsonObjectType.String;
                if (stronglyTypedIdName.Prefix is not null)
                {
                    schema.Format = $"{stronglyTypedIdName.Prefix}_{{string}}";
                }

                schema.AllOf.Clear();
                schema.Properties.Clear();
            }
        }
    }
}
