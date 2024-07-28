using NJsonSchema;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.SharedKernel.ApiCore.SchemaProcessor;

public class StronglyTypedDocumentProcessor(Assembly domainAssembly)
    : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        // Find all strongly typed IDs
        var stronglyTypedIdNames = domainAssembly.GetTypes()
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
