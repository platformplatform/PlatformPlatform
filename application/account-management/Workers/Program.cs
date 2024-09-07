using PlatformPlatform.AccountManagement;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel;

// Worker service is using WebApplication.CreateBuilder instead of Host.CreateDefaultBuilder to allow scaling to zero
var builder = WebApplication.CreateBuilder(args);

// Configure storage infrastructure like Database, BlobStorage, Entity Framework DB Context, etc.
builder
    .AddDevelopmentPort(9199)
    .AddAccountManagementInfrastructure();

// Configure dependency injection services like Repositories, MediatR, Pipelines, FluentValidation validators, etc.
builder.Services.AddAccountManagementServices();

var host = builder.Build();

// Apply migrations to the database (should be moved to GitHub Actions or similar in production)
host.Services.ApplyMigrations<AccountManagementDbContext>();

await host.RunAsync();
