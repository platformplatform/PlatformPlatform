using PlatformPlatform.Application;
using PlatformPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();