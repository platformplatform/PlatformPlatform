using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddWebApiServices();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

app.MapGet("/", () => "Hello World!");

app.Run();