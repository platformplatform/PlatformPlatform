using Azure.Core;
using PlatformPlatform.AppGateway.Filters;
using PlatformPlatform.AppGateway.Transformations;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = WebApplication.CreateBuilder(args);

var reverseProxyBuilder = builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>();

if (InfrastructureCoreConfiguration.IsRunningInAzure)
{
    builder.Services.AddSingleton<TokenCredential>(InfrastructureCoreConfiguration.GetDefaultAzureCredential());
    builder.Services.AddSingleton<ManagedIdentityTransform>();
    builder.Services.AddSingleton<ApiVersionHeaderTransform>();
    reverseProxyBuilder.AddTransforms(context =>
        {
            context.RequestTransforms.Add(context.Services.GetRequiredService<ManagedIdentityTransform>());
            context.RequestTransforms.Add(context.Services.GetRequiredService<ApiVersionHeaderTransform>());
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

builder.Services.AddNamedBlobStorages(builder, ("avatars-storage", "AVATARS_STORAGE_URL"));

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

var app = builder.Build();

// Adds middleware for redirecting HTTP Requests to HTTPS
app.UseHttpsRedirection();

if (!app.Environment.IsDevelopment())
{
    // Adds middleware for using HSTS, which adds the Strict-Transport-Security header
    // Defaults to 30 days. See https://aka.ms/aspnetcore-hsts, so be careful during development
    app.UseHsts();
}

app.MapReverseProxy();

app.Run();
