using PlatformPlatform.AppGateway;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>();

builder.WebHost.UseKestrel(option => option.AddServerHeader = false);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();