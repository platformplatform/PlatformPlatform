using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Middleware;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;
using PlatformPlatform.SharedKernel.Tests.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Tests.Api;

public abstract class BaseApiTests<TContext> : BaseTest<TContext> where TContext : DbContext
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    protected BaseApiTests()
    {
        Environment.SetEnvironmentVariable(WebAppMiddlewareConfiguration.PublicUrlKey, "https://localhost:8444");
        Environment.SetEnvironmentVariable(WebAppMiddlewareConfiguration.CdnUrlKey, "https://localhost:8444");

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace the default DbContext in the WebApplication to use an in-memory SQLite database 
                services.Remove(services.Single(d => d.ServiceType == typeof(DbContextOptions<TContext>)));
                services.AddDbContext<TContext>(options => { options.UseSqlite(Connection); });

                TelemetryEventsCollectorSpy = new TelemetryEventsCollectorSpy(new TelemetryEventsCollector());
                services.AddScoped<ITelemetryEventsCollector>(_ => TelemetryEventsCollectorSpy);
            });
        });

        TestHttpClient = _webApplicationFactory.CreateClient();
    }

    protected HttpClient TestHttpClient { get; }

    protected override void Dispose(bool disposing)
    {
        _webApplicationFactory.Dispose();
        base.Dispose(disposing);
    }

    protected static void EnsureSuccessGetRequest(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    protected static async Task EnsureSuccessPostRequest(
        HttpResponseMessage response,
        string? exact = null,
        string? startsWith = null
    )
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

    protected static void EnsureSuccessWithEmptyHeaderAndLocation(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();
    }

    [UsedImplicitly]
    protected Task EnsureErrorStatusCode(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        IEnumerable<ErrorDetail> expectedErrors
    )
    {
        return EnsureErrorStatusCode(response, statusCode, null, expectedErrors);
    }

    protected async Task EnsureErrorStatusCode(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string? expectedDetail,
        IEnumerable<ErrorDetail>? expectedErrors = null,
        bool hasTraceId = false
    )
    {
        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problemDetails = await DeserializeProblemDetails(response);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)statusCode);
        problemDetails.Type.Should().StartWith("https://tools.ietf.org/html/rfc9110#section-15.");
        problemDetails.Title.Should().Be(ApiResult.GetHttpStatusDisplayName(statusCode));

        if (expectedDetail is not null)
        {
            problemDetails.Detail.Should().Be(expectedDetail);
        }

        if (expectedErrors is not null)
        {
            var actualErrorsJson = (JsonElement)problemDetails.Extensions["Errors"]!;
            var actualErrors =
                JsonSerializer.Deserialize<ErrorDetail[]>(actualErrorsJson.GetRawText(), JsonSerializerOptions);

            actualErrors.Should().BeEquivalentTo(expectedErrors);
        }

        if (hasTraceId)
        {
            problemDetails.Extensions["traceId"]!.ToString().Should().NotBeEmpty();
        }
    }

    private async Task<ProblemDetails?> DeserializeProblemDetails(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<ProblemDetails>(content, JsonSerializerOptions);
    }
}