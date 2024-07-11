using PlatformPlatform.AccountManagement.Api;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApiCore.SinglePageApp;
using PlatformPlatform.SharedKernel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and Api layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApiServices()
    .AddApiCoreServices(builder, Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly())
    .AddConfigureStorage(builder)
    .AddSinglePageAppFallback()
    .ConfigureDevelopmentPort(builder, 9100);

var app = builder.Build();

// Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
app.UseApiCoreConfiguration();

// Server the SPA and static files if no other endpoints are found
app.UseSinglePageAppFallback();

// Apply migrations to the database (should be moved to GitHub Actions or similar in production)

Task.Run(() => app.Services.ApplyMigrationsAsync<AccountManagementDbContext>());

app.Run();
