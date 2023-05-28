using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;
using PlatformPlatform.AccountManagement.WebApi.Tenants;
using PlatformPlatform.SharedKernel.ApiCore;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and WebApi layers.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddWebApiServices();

var app = builder.Build();

// Add configuration common for all web applications like Swagger, HSTS, and UseDeveloperExceptionPage.
app.AddCommonConfiguration();

// Map tenant-related endpoints.
app.MapTenantEndpoints();

// Run the web application.
app.Run();