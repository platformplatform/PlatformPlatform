using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApiCore;
using PlatformPlatform.SharedKernel.ApiCore.SinglePageApp;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and Api layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .AddApiCoreServices(builder, Assembly.GetExecutingAssembly(), DomainConfiguration.Assembly)
    .AddConfigureStorage(builder)
    .AddSinglePageAppFallback()
    .ConfigureDevelopmentPort(builder, 9100);

var app = builder.Build();

// Add common configuration for all APIs like Swagger, HSTS, and DeveloperExceptionPage.
app.UseApiCoreConfiguration();

// Server the SPA and static files if no other endpoints are found
app.UseSinglePageAppFallback();

app.Run();
