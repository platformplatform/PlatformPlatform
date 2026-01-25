using PlatformPlatform.Account;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.Database;

// Worker service is using WebApplication.CreateBuilder instead of Host.CreateDefaultBuilder to allow scaling to zero
var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddDevelopmentPort(9199)
    .AddAccountInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddWorkerServices()
    .AddAccountServices();

builder.Services.AddTransient<DatabaseMigrationService<AccountDbContext>>();
builder.Services.AddTransient<DataMigrationRunner<AccountDbContext>>();

var host = builder.Build();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

using var scope = host.Services.CreateScope();

// Apply migrations to the database only when running locally
if (!SharedInfrastructureConfiguration.IsRunningInAzure)
{
    var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService<AccountDbContext>>();
    migrationService.ApplyMigrations();
}

var dataMigrationRunner = scope.ServiceProvider.GetRequiredService<DataMigrationRunner<AccountDbContext>>();
await dataMigrationRunner.RunMigrationsAsync(lifetime.ApplicationStopping);

await host.RunAsync();
