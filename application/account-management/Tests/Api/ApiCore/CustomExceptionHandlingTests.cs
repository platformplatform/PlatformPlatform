using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.ApiCore;

public class CustomExceptionHandlingTests : BaseApiTests<AccountManagementDbContext>
{
    private readonly WebApplicationFactory<Program> _webApplicationFactory = new();

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
                // Set the environment variable to enable the test-specific /api/throwException endpoint.
                Environment.SetEnvironmentVariable("TestEndpointsEnabled", "true");
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/throwException");

        // Assert
        if (environment == "Development")
        {
            // In Development we use app.UseDeveloperExceptionPage() which returns a HTML response.
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
            var errorResponse = await response.Content.ReadAsStringAsync();
            errorResponse.Contains("Dummy endpoint for testing.").Should().BeTrue();
        }
        else
        {
            // In Production we use GlobalExceptionHandlerMiddleware which returns a JSON response.
            await EnsureErrorStatusCode(
                response,
                HttpStatusCode.InternalServerError,
                "An error occurred while processing the request."
            );
        }
    }
}