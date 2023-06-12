using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Tests.Api;

public abstract partial class BaseApiTests<TContext> : BaseTest<TContext>, IDisposable where TContext : DbContext
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

    protected async Task EnsureErrorStatusCode(HttpResponseMessage response, HttpStatusCode statusCode,
        IEnumerable<ErrorDetail> expectedErrors)
    {
        await EnsureErrorStatusCode(response, statusCode, null, expectedErrors);
    }

    protected async Task EnsureErrorStatusCode(HttpResponseMessage response, HttpStatusCode statusCode,
        string? expectedDetail, IEnumerable<ErrorDetail>? expectedErrors = null)
    {
        response.StatusCode.Should().Be(statusCode);

        var problemDetails = await DeserializeProblemDetails(response);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int) statusCode);
        problemDetails.Type.Should().Be($"https://httpstatuses.com/{(int) statusCode}");
        problemDetails.Title.Should().Be(SplitCamelCaseTitle(statusCode.ToString()));

        if (expectedDetail is not null)
        {
            problemDetails.Detail.Should().Be(expectedDetail);
        }

        if (expectedErrors is not null)
        {
            var actualErrorsJson = (JsonElement) problemDetails.Extensions["Errors"]!;
            var options = new JsonSerializerOptions {PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
            var actualErrors = JsonSerializer.Deserialize<ErrorDetail[]>(actualErrorsJson.GetRawText(), options);

            actualErrors.Should().BeEquivalentTo(expectedErrors);
        }
    }

    private static async Task<ProblemDetails?> DeserializeProblemDetails(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProblemDetails>(content);
    }

    private static string SplitCamelCaseTitle(string title)
    {
        return SplitCamelCase().Replace(title, " $1");
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex SplitCamelCase();
}