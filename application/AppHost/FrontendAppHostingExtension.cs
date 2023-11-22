using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace AppHost;

internal class FrontendAppAddPortLifecycleHook : IDistributedApplicationLifecycleHook
{
    public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        var frontendApps = appModel.Resources.OfType<FrontendAppResource>();
        foreach (var frontendApp in frontendApps)
        {
            if (!frontendApp.TryGetServiceBindings(out var bindings)) continue;

            var environmentAnnotation = new EnvironmentCallbackAnnotation(env =>
            {
                foreach (var binding in bindings)
                {
                    var serviceName = $"{frontendApp.Name}_{binding.Name}";
                    env[$"PORT_{binding.Name.ToUpperInvariant()}"] = $"{{{{- portForServing \"{serviceName}\" -}}}}";
                }
            });

            frontendApp.Annotations.Add(environmentAnnotation);
        }

        return Task.CompletedTask;
    }
}

internal static class FrontendAppHostingExtension
{
    public static IResourceBuilder<FrontendAppResource> AddBunApp(
        this IDistributedApplicationBuilder builder,
        string name,
        string workingDirectory,
        string bunCommand
    )
    {
        var resource = new FrontendAppResource(name, "bun", workingDirectory, new[] { "run", bunCommand });

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDistributedApplicationLifecycleHook,
            FrontendAppAddPortLifecycleHook>());

        return builder.AddResource(resource)
            .WithOtlpExporter()
            .WithEnvironment("NODE_ENV", builder.Environment.IsDevelopment() ? "development" : "production")
            .ExcludeFromManifest();
    }
}

internal class FrontendAppResource(string name, string command, string workingDirectory, string[]? args)
    : ExecutableResource(name, command, workingDirectory, args);