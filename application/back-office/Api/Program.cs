using PlatformPlatform.BackOffice;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.SinglePageApp;

var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddApiInfrastructure()
    .AddDevelopmentPort(9200)
    .AddBackOfficeInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddApiServices(Assembly.GetExecutingAssembly(), Configuration.Assembly)
    .AddBackOfficeServices()
    .AddSinglePageAppFallback();

var app = builder.Build();

app
    .UseApiServices() // Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
    .UseSinglePageAppFallback(); // Server the SPA and static files if no other endpoints are found

await app.RunAsync();
