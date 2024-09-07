using Azure.Core;
using PlatformPlatform.AppGateway.Filters;
using PlatformPlatform.AppGateway.Middleware;
using PlatformPlatform.AppGateway.Transformations;
using PlatformPlatform.SharedKernel;

var builder = WebApplication.CreateBuilder(args);

var reverseProxyBuilder = builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>();

if (SharedDependencyConfiguration.IsRunningInAzure)
{
    builder.Services.AddSingleton<TokenCredential>(SharedDependencyConfiguration.DefaultAzureCredential);
    builder.Services.AddSingleton<ManagedIdentityTransform>();
    builder.Services.AddSingleton<ApiVersionHeaderTransform>();
    builder.Services.AddSingleton<HttpStrictTransportSecurityTransform>();
    reverseProxyBuilder.AddTransforms(context =>
        {
            context.RequestTransforms.Add(context.Services.GetRequiredService<ManagedIdentityTransform>());
            context.RequestTransforms.Add(context.Services.GetRequiredService<ApiVersionHeaderTransform>());
            context.ResponseTransforms.Add(context.Services.GetRequiredService<HttpStrictTransportSecurityTransform>());
        }
    );
}
else
{
    builder.Services.AddSingleton<SharedAccessSignatureRequestTransform>();
    reverseProxyBuilder.AddTransforms(context =>
        context.RequestTransforms.Add(context.Services.GetRequiredService<SharedAccessSignatureRequestTransform>())
    );
}

builder.Services.AddSingleton(SharedDependencyConfiguration.GetTokenSigningService());

builder.Services.AddHttpClient(
    "AccountManagement",
    client => { client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ACCOUNT_MANAGEMENT_API_URL") ?? "https://localhost:9100"); }
);

builder.Services
    .AddSingleton<BlockInternalApiTransform>()
    .AddSingleton<AuthenticationCookieMiddleware>();

reverseProxyBuilder.AddTransforms(context =>
    context.RequestTransforms.Add(context.Services.GetRequiredService<BlockInternalApiTransform>())
);

builder.AddNamedBlobStorages(("avatars-storage", "AVATARS_STORAGE_URL"));

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

var app = builder.Build();

app.MapReverseProxy();

app.UseMiddleware<AuthenticationCookieMiddleware>();

await app.RunAsync();
