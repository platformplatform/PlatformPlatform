using PlatformPlatform.AccountManagement.Api.Tenants;
using PlatformPlatform.AccountManagement.Api.Users;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and Api layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddDatabaseContext(builder)
    .AddInfrastructureServices()
    .AddApiCoreServices(builder)
    .AddWebAppMiddleware();

var app = builder.Build();

// Add common configuration for all APIs like Swagger, HSTS, DeveloperExceptionPage, and run EF database migrations.
app.AddApiCoreConfiguration<AccountManagementDbContext>();
app.UseWebAppMiddleware();

app.MapTenantEndpoints();
app.MapUserEndpoints();

app.Run();