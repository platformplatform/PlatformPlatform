using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.SharedKernel.ApiResults;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.SharedKernel.Tests;

public static class ApiTestHelpers
{
    public static void EnsureSuccessGetRequest(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    public static async Task EnsureSuccessPostRequest(
        HttpResponseMessage response,
        string? exact = null,
        string? startsWith = null,
        bool hasLocation = true
    )
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().BeEmpty();

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();

        if (hasLocation)
        {
            response.Headers.Location.Should().NotBeNull();
        }
        else
        {
            response.Headers.Location.Should().BeNull();
        }

        if (exact is not null)
        {
            response.Headers.Location!.ToString().Should().Be(exact);
        }

        if (startsWith is not null)
        {
            response.Headers.Location!.ToString().StartsWith(startsWith).Should().BeTrue();
        }
    }

    public static void EnsureSuccessWithEmptyHeaderAndLocation(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();
    }

    public static Task EnsureErrorStatusCode(HttpResponseMessage response, HttpStatusCode statusCode, IEnumerable<ErrorDetail> expectedErrors)
    {
        return EnsureErrorStatusCode(response, statusCode, null, expectedErrors);
    }

    public static async Task EnsureErrorStatusCode(
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
            var actualErrors = JsonSerializer.Deserialize<ErrorDetail[]>(actualErrorsJson.GetRawText(), InfrastructureCoreConfiguration.JsonSerializerOptions);

            actualErrors.Should().BeEquivalentTo(expectedErrors);
        }

        if (hasTraceId)
        {
            problemDetails.Extensions["traceId"]!.ToString().Should().NotBeEmpty();
        }
    }

    public static async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
    {
        var responseStream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<T>(responseStream, InfrastructureCoreConfiguration.JsonSerializerOptions);
    }

    private static async Task<ProblemDetails?> DeserializeProblemDetails(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<ProblemDetails>(content, InfrastructureCoreConfiguration.JsonSerializerOptions);
    }
}
