using AppGateway;
using AppGateway.ApiAggregation;
using AppGateway.Filters;
using AppGateway.Middleware;
using AppGateway.Transformations;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using SharedKernel.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Wire OpenTelemetry, Azure Monitor exporter, and Application Insights so AppGateway requests,
// dependencies, traces, and logs surface in the cluster's Application Insights workspace alongside the APIs.
builder.AddSharedTelemetry();

builder.Services
    .AddOptions<HostnamesOptions>()
    .Bind(builder.Configuration.GetSection(HostnamesOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.App), "Hostnames:App must be configured.")
    .ValidateOnStart();

// PortAllocation reads .workspace/port.txt by walking up to the repo root for .git, which doesn't
// exist in Azure Container Apps (working dir /app). Locally we load the real allocation; in Azure we
// register a sentinel so downstream consumers (YARP filters, middleware, HttpClient factories) still
// resolve the dependency, while their {SERVICE}_API_URL env-var paths take priority over the unused
// port values.
builder.Services.AddSingleton(SharedInfrastructureConfiguration.IsRunningInAzure
    ? new PortAllocation(0)
    : PortAllocation.Load()
);

var reverseProxyBuilder = builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>()
    .AddConfigFilter<ApiExplorerRouteFilter>()
    .AddConfigFilter<HostMatchConfigFilter>()
    .AddTransforms(context => context.RequestTransforms.Add(context.Services.GetRequiredService<BlockInternalApiTransform>()));

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
    reverseProxyBuilder.AddTransforms(context => context.RequestTransforms.Add(context.Services.GetRequiredService<SharedAccessSignatureRequestTransform>())
    );
}

builder.AddNamedBlobStorages([("account-storage", "ACCOUNT_STORAGE_URL")]);

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

builder.Services.AddHttpClient("Account", (sp, client) =>
    {
        var ports = sp.GetRequiredService<PortAllocation>();
        var productionUrl = Environment.GetEnvironmentVariable("ACCOUNT_API_URL");
        client.BaseAddress = !string.IsNullOrEmpty(productionUrl)
            ? new Uri(productionUrl)
            : new Uri($"https://localhost:{ports.AccountApi}");
        // Allow cold-start refreshes to complete and deliver the new Set-Cookie back to the browser.
        // The browser has no client-side timeout, so the gateway is the authoritative timeout for refresh.
        client.Timeout = TimeSpan.FromSeconds(60);
    }
);

builder.Services
    .AddHttpClient()
    .AddHttpForwardHeaders() // Ensure the correct client IP addresses are set for downstream requests
    .AddOutputCache();

builder.Services
    .AddSingleton(SharedDependencyConfiguration.GetTokenSigningService())
    .AddSingleton<BlockInternalApiTransform>()
    .AddSingleton<LocalhostRedirectMiddleware>()
    .AddSingleton<AuthenticationCookieMiddleware>()
    .AddScoped<ApiAggregationService>();

var app = builder.Build();

app.ApiAggregationEndpoints();

app.UseForwardedHeaders() // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto. Should run before other middleware.
    .UseMiddleware<LocalhostRedirectMiddleware>()
    .UseOutputCache()
    .UseMiddleware<AuthenticationCookieMiddleware>();

app.MapScalarApiReference("/openapi", options =>
    {
        options
            .WithOpenApiRoutePattern("/openapi/v1.json")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithTitle("PlatformPlatform API");
    }
);

app.MapReverseProxy();

app.MapFallback((HttpContext context, IOptions<HostnamesOptions> hostnameOptions, PortAllocation ports) =>
    {
        var hostnames = hostnameOptions.Value;
        // Port is only meaningful for local Aspire stacks (non-standard ports). In Azure both the
        // request Host.Port and the sentinel PortAllocation(0) leave us at 0; drop the suffix so the
        // canonical URLs resolve to the standard https port behind ACA ingress.
        var port = context.Request.Host.Port ?? ports.AppGateway;
        var portSuffix = port == 0 ? string.Empty : $":{port}";
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Unknown host",
            Detail = $"The host '{context.Request.Host}' is not recognized. Use one of the canonical URLs.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            Extensions =
            {
                ["canonicalUrls"] = new[] { $"https://{hostnames.App}{portSuffix}" }
            }
        };

        return Results.Problem(problemDetails);
    }
);

await app.RunAsync();
