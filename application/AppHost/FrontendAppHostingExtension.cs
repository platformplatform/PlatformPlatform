using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AppHost;

internal class FrontendAppAddPortLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var frontendApps = appModel.Resources.OfType<ExecutableResource>();
        foreach (var frontendApp in frontendApps)
        {
            if (!frontendApp.TryGetServiceBindings(out var bindings)) continue;

            var environmentAnnotation = new EnvironmentCallbackAnnotation(env =>
            {
                foreach (var bindingName in bindings.Select(b => b.Name))
                {
                    var serviceName = $"{frontendApp.Name}_{bindingName}";
                    env[$"PORT_{bindingName.ToUpperInvariant()}"] = $"{{{{- portForServing \"{serviceName}\" -}}}}";
                }
            });

            frontendApp.Annotations.Add(environmentAnnotation);
        }

        return Task.CompletedTask;
    }
}

internal static class FrontendAppHostingExtension
{
    public static IResourceBuilder<ExecutableResource> AddFrontendApp(
        this IDistributedApplicationBuilder builder,
        string name,
        string workingDirectory,
        string npmCommand
    )
    {
        var resource = new ExecutableResource(name, "yarn", workingDirectory, ["run", npmCommand]);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDistributedApplicationLifecycleHook, FrontendAppAddPortLifecycleHook>()
        );

        return builder.AddResource(resource)
            .WithOtlpExporter()
            .WithEnvironment("NODE_ENV", builder.Environment.IsDevelopment() ? "development" : "production")
            .ExcludeFromManifest();
    }
}