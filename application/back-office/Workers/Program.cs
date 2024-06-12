using PlatformPlatform.BackOffice.Application;
using PlatformPlatform.BackOffice.Infrastructure;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HostOptions>(options =>
    {
        options.ServicesStartConcurrently = true;
        options.StartupTimeout = TimeSpan.FromSeconds(60);
        
        options.ServicesStopConcurrently = true;
        options.ShutdownTimeout = TimeSpan.FromSeconds(10);
    }
);

// Configure services for the Application, Infrastructure layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .AddConfigureStorage(builder);

var host = builder.Build();

// Apply migrations to the database (should be moved to GitHub Actions or similar in production)
host.Services.ApplyMigrations<BackOfficeDbContext>();

host.Run();
