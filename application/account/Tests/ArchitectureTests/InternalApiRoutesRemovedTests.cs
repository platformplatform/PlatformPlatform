using System.Net;
using System.Net.Http.Json;
using Account.Database;
using FluentAssertions;
using Xunit;

namespace Account.Tests.ArchitectureTests;

// Regression guard for PP-1251. The /internal-api/account/feature-flags/* routes were unauthenticated
// and unhost-pinned, and the legacy /internal-api/account/tenants GET exposed cross-tenant data.
// The InternalApiEndpoints_ShouldEitherRequireAuthorizationOrBeOnAllowlist arch test enforces the
// auth-or-allowlist invariant going forward; this Theory locks in that the specific routes deleted
// by PP-1251 stay deleted (a future contributor cannot accidentally remap them).
public sealed class InternalApiRoutesRemovedTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
{
    [Theory]
    [InlineData("GET", "/internal-api/account/feature-flags")]
    [InlineData("GET", "/internal-api/account/feature-flags/beta-features/tenants")]
    [InlineData("GET", "/internal-api/account/feature-flags/beta-features/users")]
    [InlineData("PUT", "/internal-api/account/feature-flags/beta-features/activate")]
    [InlineData("PUT", "/internal-api/account/feature-flags/beta-features/deactivate")]
    [InlineData("PUT", "/internal-api/account/feature-flags/beta-features/tenant-override")]
    [InlineData("PUT", "/internal-api/account/feature-flags/beta-features/rollout-percentage")]
    [InlineData("DELETE", "/internal-api/account/feature-flags/beta-features/tenant-override")]
    [InlineData("PUT", "/internal-api/account/feature-flags/beta-features/user-override")]
    [InlineData("DELETE", "/internal-api/account/feature-flags/beta-features/user-override")]
    [InlineData("GET", "/internal-api/account/tenants")]
    public async Task DeletedInternalApiRoute_ShouldReturnNotFound(string method, string path)
    {
        // Act - anonymous client because these routes were anonymous when they existed; if the route
        // were silently re-registered with auth, an anonymous caller would see 401 not 404 and the
        // test would still fail loudly.
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "PUT" or "POST") request.Content = JsonContent.Create(new { });
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, $"the {method} {path} route was deleted by PP-1251 and must not be re-introduced");
    }
}
