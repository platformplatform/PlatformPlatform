using Azure.Core;
using PlatformPlatform.AppGateway;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = WebApplication.CreateBuilder(args);

if (InfrastructureCoreConfiguration.IsRunningInAzure)
{
    builder.Services.AddSingleton<ManagedIdentityTransform>();
    builder.Services.AddSingleton<ApiVersionHeaderTransform>();
    builder.Services.AddSingleton<TokenCredential>(InfrastructureCoreConfiguration.GetDefaultAzureCredential());
}
else
{
    builder.Services.AddSingleton<SharedAccessSignatureRequestTransform>();
}

builder.Services.AddNamedBlobStorages(builder, ("avatars-storage", "AVATARS_STORAGE_URL"));

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>()
    .AddTransforms(context =>
        {
            if (InfrastructureCoreConfiguration.IsRunningInAzure)
            {
                context.RequestTransforms.Add(context.Services.GetRequiredService<ManagedIdentityTransform>());
                context.RequestTransforms.Add(context.Services.GetRequiredService<ApiVersionHeaderTransform>());
            }
            else
            {
                context.RequestTransforms.Add(context.Services.GetRequiredService<SharedAccessSignatureRequestTransform>());
            }
        }
    );

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();
