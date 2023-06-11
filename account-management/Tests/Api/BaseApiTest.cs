using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.AccountManagement.Tests.Api;

public abstract class BaseApiTests<TContext> : BaseTest<TContext>, IDisposable where TContext : DbContext
{
    // This string represents a custom DateTime format based on the built-in format "o".
    // The format "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK" is used to avoid trailing zeros in the DateTime string.
    // The 'F's in the format are upper-case to indicate that trailing zeros should be removed.
    // See https://stackoverflow.com/a/17349663
    protected const string Iso8601TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK";

    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    protected BaseApiTests()
    {
        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's DbContext registration.
                var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<TContext>));
                services.Remove(descriptor);

                // Add DbContext using in-memory Sqlite database
                services.AddDbContext<TContext>(options => { options.UseSqlite(Connection); });

                services.AddTransient<DatabaseSeeder>();
            });
        });

        var serviceScope = _webApplicationFactory.Services.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        TestHttpClient = _webApplicationFactory.CreateClient();
    }

    protected HttpClient TestHttpClient { get; }

    public new void Dispose()
    {
        _webApplicationFactory.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected static void EnsureSuccessGetRequest(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    protected static async Task EnsureSuccessPostRequest(HttpResponseMessage response, string? exact = null,
        string? startsWith = null)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().BeEmpty();

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().NotBeNull();
        if (exact is not null)
        {
            response.Headers.Location!.ToString().Should().Be(exact);
        }

        if (startsWith is not null)
        {
            response.Headers.Location!.ToString().StartsWith(startsWith).Should().BeTrue();
        }
    }

    protected static void EnsureSuccessPutRequest(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();
    }

    protected static void EnsureSuccessDeleteRequest(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();
    }

    protected static async Task EnsureErrorStatusCode(HttpResponseMessage response,
        HttpStatusCode httpStatusCode, string? expectedBody = null)
    {
        response.StatusCode.Should().Be(httpStatusCode);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();

        if (expectedBody is not null)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            responseBody.Should().Be(expectedBody);
        }
    }
}