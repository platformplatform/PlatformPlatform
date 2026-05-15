using Account;
using Account.Database;
using Account.Workers;
using SharedKernel.Configuration;
using SharedKernel.Database;

// Worker service is using WebApplication.CreateBuilder instead of Host.CreateDefaultBuilder to allow scaling to zero
var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Logging, Telemetry, Entity Framework DB Context, etc.
builder
    .AddDevelopmentPort()
    .AddAccountInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services
    .AddWorkerServices()
    .AddAccountServices();

builder.Services.AddTransient<DatabaseMigrationService<AccountDbContext>>();
builder.Services.AddTransient<DataMigrationRunner<AccountDbContext>>();
builder.Services.AddTransient<FeatureFlagDefinitionReconciler>();

builder.Services.AddHostedService<BillingDriftWorker>();

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

// Converge the feature_flags table to the C# definitions on every Worker startup. Must complete
// successfully before the worker accepts traffic - if reconciliation throws, the process exits
// non-zero so the orchestrator notices, which is preferable to running with inconsistent flag state.
var featureFlagDefinitionReconciler = scope.ServiceProvider.GetRequiredService<FeatureFlagDefinitionReconciler>();
await featureFlagDefinitionReconciler.ReconcileAsync(lifetime.ApplicationStopping);

await host.RunAsync();
