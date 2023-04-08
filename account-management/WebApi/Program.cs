using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .AddWebApiServices();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();