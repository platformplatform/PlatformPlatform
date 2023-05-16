using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi;
using PlatformPlatform.AccountManagement.WebApi.Endpoints;
using PlatformPlatform.Foundation;
using PlatformPlatform.Foundation.WebApi;

var builder = WebApplication.CreateBuilder(args);

// Configure services for the Application, Infrastructure, and WebApi layers.
builder.Services
    .AddFoundationServices()
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddWebApiServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable the developer exception page, which displays detailed information about exceptions that occur.
    app.UseDeveloperExceptionPage();

    // Enable Swagger UI in the development environment.
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AccountManagement API - Unstable"));
}
else
{
    // Adds middleware for using HSTS, which adds the Strict-Transport-Security header
    // Defaults to 30 days. See https://aka.ms/aspnetcore-hsts, so be careful during development.
    app.UseHsts();

    // Adds middleware for redirecting HTTP Requests to HTTPS.
    app.UseHttpsRedirection();

    // Configure global exception handling for the production environment.
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
}

// Add a default "Hello World!" endpoint at the root path.
app.MapGet("/", () => "Hello World!").ExcludeFromDescription();

// Map tenant-related endpoints.
app.MapTenantEndpoints();

// Add test-specific endpoints when running tests, such as /throwException.
app.MapTestEndpoints();

// Run the web application.
app.Run();