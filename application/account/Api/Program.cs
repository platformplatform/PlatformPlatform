using PlatformPlatform.Account;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.SinglePageApp;

var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddApiInfrastructure()
    .AddDevelopmentPort(9100)
    .AddAccountInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddApiServices([Assembly.GetExecutingAssembly(), Configuration.Assembly])
    .AddAccountServices();

var app = builder.Build();

app
    .UseApiServices() // Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
    .UseFederatedModuleStaticFiles(); // Serve federated module files (remoteEntry.js, JS/CSS bundles)

await app.RunAsync();
