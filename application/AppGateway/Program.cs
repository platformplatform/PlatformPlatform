using PlatformPlatform.AppGateway;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddConfigFilter<ClusterDestinationConfigFilter>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();