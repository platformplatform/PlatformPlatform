namespace AppHost;

public static class ConfigurationExtensions
{
    extension<TDestination>(IResourceBuilder<TDestination> builder) where TDestination : IResourceWithEnvironment
    {
        public IResourceBuilder<TDestination> WithUrlConfiguration(string applicationBasePath)
        {
            var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_URL") ?? "https://localhost:9000";
            applicationBasePath = applicationBasePath.TrimEnd('/');

            return builder
                .WithEnvironment("PUBLIC_URL", baseUrl)
                .WithEnvironment("CDN_URL", baseUrl + applicationBasePath);
        }
    }
}
