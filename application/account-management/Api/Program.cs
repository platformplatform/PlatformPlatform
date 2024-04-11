using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.InfrastructureCore;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and Api layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .AddApiCoreServices(builder, Assembly.GetExecutingAssembly(), DomainConfiguration.Assembly)
    .ConfigureStorage(builder);

var app = builder.Build();

// Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
app.AddApiCoreConfiguration();

// Apply migrations to the database (should be move to GitHub Actions or similar in production)
app.Services.ApplyMigrations<AccountManagementDbContext>();

app.Run();
