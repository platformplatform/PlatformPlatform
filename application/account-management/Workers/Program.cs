using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Configure services for the Application, Infrastructure layers like Entity Framework, Repositories, MediatR,
// FluentValidation validators, Pipelines.
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices()
    .ConfigureStorage(builder);

var host = builder.Build();
host.Run();
