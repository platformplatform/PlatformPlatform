using Azure.Core;
using PlatformPlatform.AppGateway.ApiAggregation;
using PlatformPlatform.AppGateway.Filters;
using PlatformPlatform.AppGateway.Middleware;
using PlatformPlatform.AppGateway.Transformations;
using PlatformPlatform.SharedKernel.Configuration;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var reverseProxyBuilder = builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>()
    .AddConfigFilter<ApiExplorerRouteFilter>();

if (SharedInfrastructureConfiguration.IsRunningInAzure)
{
    builder.Services.AddSingleton<TokenCredential>(SharedInfrastructureConfiguration.DefaultAzureCredential);
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

// Ensure correct client IP addresses are set for requests
builder.Services.AddHttpForwardHeaders();

reverseProxyBuilder.AddTransforms(context =>
    context.RequestTransforms.Add(context.Services.GetRequiredService<BlockInternalApiTransform>())
);

builder.AddNamedBlobStorages(("avatars-storage", "AVATARS_STORAGE_URL"));

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiAggregationService>();
builder.Services.AddOutputCache();

var app = builder.Build();

// Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto. Should run before other middleware.
app.UseForwardedHeaders();

app.UseOutputCache();

app.ApiAggregationEndpoints();

app.MapScalarApiReference(options =>
    {
        options
            .WithEndpointPrefix("/openapi/{documentName}")
            .WithOpenApiRoutePattern("/openapi/v1.json")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithTitle("PlatformPlatform API")
            .WithSidebar(true);
    }
);

app.MapReverseProxy();

app.UseMiddleware<AuthenticationCookieMiddleware>();

await app.RunAsync();
