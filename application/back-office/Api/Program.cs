using PlatformPlatform.BackOffice;
using PlatformPlatform.SharedKernel;
using PlatformPlatform.SharedKernel.SinglePageApp;

var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddApiInfrastructure()
    .AddDevelopmentPort(9200)
    .AddBackOfficeInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddApiServices(Assembly.GetExecutingAssembly(), [DependencyConfiguration.Assembly])
    .AddBackOfficeServices()
    .AddSinglePageAppFallback();

var app = builder.Build();

// Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
app.UseApiServices();

// Server the SPA and static files if no other endpoints are found
app.UseSinglePageAppFallback();

await app.RunAsync();
