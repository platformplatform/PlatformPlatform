using Azure.Core;
using Microsoft.AspNetCore.HttpOverrides;
using PlatformPlatform.AppGateway.Filters;
using PlatformPlatform.AppGateway.Middleware;
using PlatformPlatform.AppGateway.Transformations;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = WebApplication.CreateBuilder(args);

var reverseProxyBuilder = builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>();

if (InfrastructureCoreConfiguration.IsRunningInAzure)
{
    builder.Services.AddSingleton<TokenCredential>(InfrastructureCoreConfiguration.DefaultAzureCredential);
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

var authenticationTokenSettings = builder.Configuration.GetSection("AuthenticationTokenSettings").Get<AuthenticationTokenSettings>()
                                  ?? throw new InvalidOperationException("No AuthenticationTokenSettings configuration found.");
builder.Services.AddSingleton(authenticationTokenSettings);

builder.Services.AddHttpClient("AccountManagement", client => { client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ACCOUNT_MANAGEMENT_API_URL") ?? "https://localhost:9100"); }
);

builder.Services
    .AddSingleton<BlockInternalApiTransform>()
    .AddSingleton<AuthenticationCookieMiddleware>();

reverseProxyBuilder.AddTransforms(context =>
    context.RequestTransforms.Add(context.Services.GetRequiredService<BlockInternalApiTransform>())
);

builder.Services.AddNamedBlobStorages(builder, ("avatars-storage", "AVATARS_STORAGE_URL"));

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

// Ensure correct client IP addresses are set for requests
// This is required when running behind a reverse proxy like YARP or Azure Container Apps
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
);

builder.Services.ConfigureDataProtectionApi();

var app = builder.Build();

app.MapReverseProxy();

app.UseMiddleware<AuthenticationCookieMiddleware>();

await app.RunAsync();
