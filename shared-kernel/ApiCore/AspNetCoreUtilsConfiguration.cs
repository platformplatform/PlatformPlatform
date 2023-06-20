using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;

namespace PlatformPlatform.SharedKernel.ApiCore;

public static class AspNetCoreUtilsConfiguration
{
    [UsedImplicitly]
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddTransient<GlobalExceptionHandlerMiddleware>();
        services.AddTransient<ModelBindingExceptionHandlerMiddleware>();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo {Title = "PlatformPlatform API", Version = "v1"});
            // This is needed because commands are nested so CreateTenant.Command becomes CreateTenant+Command 
            c.CustomSchemaIds(type => type.FullName?.Split(".").Last().Replace("+", ""));
        });

        // Ensure that enums are serialized as strings
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }

    [UsedImplicitly]
    public static WebApplication AddCommonConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            // Enable the developer exception page, which displays detailed information about exceptions that occur.
            app.UseDeveloperExceptionPage();

            // Enable Swagger UI in the development environment.
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API"));
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

        app.UseMiddleware<ModelBindingExceptionHandlerMiddleware>();

        // Add test-specific endpoints when running tests, such as /throwException.
        app.MapTestEndpoints();

        return app;
    }
}