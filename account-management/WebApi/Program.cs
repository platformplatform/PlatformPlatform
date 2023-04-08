using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.WebApi.Endpoints;

namespace PlatformPlatform.AccountManagement.WebApi;

public class Program
{
    internal static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddApplicationServices()
            .AddInfrastructureServices(builder.Configuration)
            .AddWebApiServices();

        var app = builder.Build();

        if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

        app.MapGet("/", () => "Hello World!");

        app.MapTenantEndpoints();

        app.Run();
    }
}