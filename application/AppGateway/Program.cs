using PlatformPlatform.AppGateway;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<SharedAccessSignatureRequestTransform>();
builder.Services.AddBlobStorage(builder, "account-management-storage");

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>()
    .AddTransforms(context =>
    {
        context.RequestTransforms.Add(context.Services.GetRequiredService<SharedAccessSignatureRequestTransform>());
    });

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();