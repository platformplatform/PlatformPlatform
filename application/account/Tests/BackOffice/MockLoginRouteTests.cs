using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using SharedKernel.Platform;
using Xunit;

namespace Account.Tests.BackOffice;

// Verifies the dev-only /login picker exemption. In tests SharedInfrastructureConfiguration.IsRunningInAzure
// is false (AZURE_CLIENT_ID is unset), so /login must be reachable on the back-office host without an
// authenticated principal — it serves the SPA shell that hosts the MockEasyAuth identity picker. The
// Azure branch (empty unauthenticatedPaths + 401 short-circuit) is gated on the static readonly
// SharedInfrastructureConfiguration.IsRunningInAzure and verified by inspection of Program.cs.
public sealed class MockLoginRouteTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetLogin_OnBackOfficeHostWithoutAuth_ShouldServeSpaShell()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        // Act
        var response = await client.GetAsync("/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("id=\"back-office\"");
        body.Should().Contain($"<title>{Settings.Current.Branding.ProductName} Back Office</title>");
    }
}
