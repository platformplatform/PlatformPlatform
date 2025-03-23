using PlatformPlatform.AccountManagement;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.Database;

// Worker service is using WebApplication.CreateBuilder instead of Host.CreateDefaultBuilder to allow scaling to zero
var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddDevelopmentPort(9199)
    .AddAccountManagementInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddWorkerServices()
    .AddAccountManagementServices();

builder.Services.AddTransient<DatabaseMigrationService<AccountManagementDbContext>>();

var host = builder.Build();

if (!SharedInfrastructureConfiguration.IsRunningInAzure)
{
    using var scope = host.Services.CreateScope();
    var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService<AccountManagementDbContext>>();
    migrationService.ApplyMigrations();
}

await host.RunAsync();
