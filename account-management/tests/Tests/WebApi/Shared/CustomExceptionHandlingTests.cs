using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.WebApi.Shared;

public class CustomExceptionHandlingTests
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public CustomExceptionHandlingTests()
    {
        _webApplicationFactory = new WebApplicationFactory<Program>();
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task CustomExceptionHandling_WhenThrowingException_ShouldHandleExceptionsCorrectly(string environment)
    {
        // Arrange
        var client = _webApplicationFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, environment);
            builder.ConfigureAppConfiguration((_, _) =>
            {
                // Set the environment variable to enable the test-specific /throwException endpoint.
                Environment.SetEnvironmentVariable("TestEndpointsEnabled", "true");
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/throwException");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        if (environment == "Development")
        {
            // In Development we use app.UseDeveloperExceptionPage() which returns a HTML response.
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
            var errorResponse = await response.Content.ReadAsStringAsync();
            errorResponse.Contains("Dummy endpoint for testing.").Should().BeTrue();
        }
        else
        {
            // In Production we use GlobalExceptionHandlerMiddleware which returns a JSON response.
            var errorResponse = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            errorResponse.Should().NotBeNull();
            errorResponse!.Type.Should().Be("Server Error");
            errorResponse.Title.Should().Be("Server Error");
            errorResponse.Detail.Should().Be("An error occurred while processing the request.");
        }
    }
}