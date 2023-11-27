using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PlatformPlatform.SharedKernel.ApiCore.Aspire;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApiCore.Filters;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.SharedKernel.ApiCore;

public static class ApiCoreConfiguration
{
    private const string LocalhostCorsPolicyName = "localhost8443";
    private const string LocalhostUrl = "https://localhost:8443";

    [UsedImplicitly]
    public static IServiceCollection AddApiCoreServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services
            .AddExceptionHandler<TimeoutExceptionHandler>()
            .AddExceptionHandler<GlobalExceptionHandler>()
            .AddTransient<ModelBindingExceptionHandlerMiddleware>()
            .AddProblemDetails()
            .AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "PlatformPlatform API", Version = "v1" });

            // Ensure that enums are shown as strings in the Swagger UI
            c.SchemaFilter<XEnumNamesSchemaFilter>();
        });

        // Ensure that enums are serialized as strings
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.AddServiceDefaults();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddCors(options => options.AddPolicy(
                LocalhostCorsPolicyName,
                policyBuilder => { policyBuilder.WithOrigins(LocalhostUrl).AllowAnyMethod().AllowAnyHeader(); }
            ));
        }
        else
        {
            // When running inside a Docker container running as non-root we need to use a port higher than 1024.
            builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(8443, _ => { }));
        }

        return services;
    }

    [UsedImplicitly]
    public static WebApplication AddApiCoreConfiguration<TDbContext>(this WebApplication app)
        where TDbContext : DbContext
    {
        app.MapDefaultEndpoints();

        // Enable Swagger UI
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API"));

        if (app.Environment.IsDevelopment())
        {
            // Enable the developer exception page, which displays detailed information about exceptions that occur.
            app.UseDeveloperExceptionPage();
            app.UseCors(LocalhostCorsPolicyName);
        }
        else
        {
            // Adds middleware for using HSTS, which adds the Strict-Transport-Security header
            // Defaults to 30 days. See https://aka.ms/aspnetcore-hsts, so be careful during development.
            app.UseHsts();

            // Adds middleware for redirecting HTTP Requests to HTTPS.
            app.UseHttpsRedirection();

            // Configure global exception handling for the production environment.
            app.UseExceptionHandler(_ => { });
        }

        app.UseMiddleware<ModelBindingExceptionHandlerMiddleware>();

        // Add test-specific endpoints when running tests, such as /api/throwException.
        app.MapTestEndpoints();

        app.Services.ApplyMigrations<TDbContext>();

        return app;
    }
}