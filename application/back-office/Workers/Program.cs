using PlatformPlatform.BackOffice;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel;

// Worker service is using WebApplication.CreateBuilder instead of Host.CreateDefaultBuilder to allow scaling to zero
var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddBackOfficeServices()
    .AddStorage(builder)
    .ConfigureDevelopmentPort(builder, 9299);

var host = builder.Build();

// Apply migrations to the database (should be moved to GitHub Actions or similar in production)
host.Services.ApplyMigrations<BackOfficeDbContext>();

await host.RunAsync();
