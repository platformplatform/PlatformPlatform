using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and Api layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .AddApiCoreServices(builder, Assembly.GetExecutingAssembly(), DomainConfiguration.Assembly)
    .ConfigureStorage(builder)
    .AddWebAppMiddleware();

var app = builder.Build();

// Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
app.AddApiCoreConfiguration();

// Server the SPA Index.html if no other endpoints are found
app.UseWebAppMiddleware();

app.Run();
