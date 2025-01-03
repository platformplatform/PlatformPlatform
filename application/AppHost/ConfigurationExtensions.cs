namespace AppHost;

public static class ConfigurationExtensions
{
    public static IResourceBuilder<TDestination> WithUrlConfiguration<TDestination>(
        this IResourceBuilder<TDestination> builder,
        string applicationBasePath) where TDestination : IResourceWithEnvironment
    {
        var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_URL") ?? "https://localhost:9000";
        applicationBasePath = applicationBasePath.TrimEnd('/');

        return builder
            .WithEnvironment("PUBLIC_URL", baseUrl)
            .WithEnvironment("CDN_URL", baseUrl + applicationBasePath);
    }
}
