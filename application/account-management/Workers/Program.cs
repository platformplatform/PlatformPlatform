using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = Host.CreateApplicationBuilder(args);

// Configure services for the Application, Infrastructure layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .ConfigureStorage(builder);

var host = builder.Build();

// Apply migrations to the database (should be moved to GitHub Actions or similar in production)
host.Services.ApplyMigrations<AccountManagementDbContext>();

host.Run();
