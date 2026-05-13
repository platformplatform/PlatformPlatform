using System.Net;
using System.Security.Claims;
using AppGateway.Transformations;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenSigning;
using Xunit;
using Yarp.ReverseProxy.Transforms;

namespace AppGateway.Tests;

public sealed class UserFeatureFlagsResponseTransformTests(AppGatewayApplicationFactory factory) : IClassFixture<AppGatewayApplicationFactory>
{
    [Fact]
    public async Task ApplyAsync_WhenAccessTokenStashedWithFeatureFlagsClaim_ShouldEmitHeaderWithKeys()
    {
        // Arrange
        var transform = factory.Services.GetRequiredService<UserFeatureFlagsResponseTransform>();
        var signingClient = factory.Services.GetRequiredService<ITokenSigningClient>();
        var accessToken = CreateSignedToken(signingClient, [new Claim("feature_flags", "custom-branding,compact-view")]);

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            Items = { [UserFeatureFlagsResponseTransform.CurrentAccessTokenItemKey] = accessToken }
        };
        var transformContext = new ResponseTransformContext { HttpContext = context };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be("custom-branding,compact-view");
    }

    [Fact]
    public async Task ApplyAsync_WhenAccessTokenHasNoFeatureFlagsClaim_ShouldEmitEmptyHeader()
    {
        // Arrange
        var transform = factory.Services.GetRequiredService<UserFeatureFlagsResponseTransform>();
        var signingClient = factory.Services.GetRequiredService<ITokenSigningClient>();
        var accessToken = CreateSignedToken(signingClient, []);

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            Items = { [UserFeatureFlagsResponseTransform.CurrentAccessTokenItemKey] = accessToken }
        };
        var transformContext = new ResponseTransformContext { HttpContext = context };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be(string.Empty);
    }

    [Fact]
    public async Task ApplyAsync_WhenNoAccessTokenStashed_ShouldNotEmitHeader()
    {
        // Arrange
        var transform = factory.Services.GetRequiredService<UserFeatureFlagsResponseTransform>();

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        var transformContext = new ResponseTransformContext { HttpContext = context };

        // Act
        await transform.ApplyAsync(transformContext);

        // Assert
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey);
    }

    [Fact]
    public async Task ApplyAsync_WhenUpstreamSetsRefreshAuthenticationTokensHeader_ShouldRefreshAndEmitPostRefreshHeader()
    {
        // Regression net for the prior split-design race: the transform read the pre-refresh access
        // token before OnStarting swapped cookies, so the response header reflected stale flag state.
        // Consolidating cookie-swap into the transform must produce a header that matches the
        // post-refresh claim set on the SAME response.
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var transform = stubFactory.Services.GetRequiredService<UserFeatureFlagsResponseTransform>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();

        var inboundRefreshToken = CreateSignedToken(signingClient, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, [new Claim("feature_flags", "stale-flag")]);
        var postRefreshAccessToken = CreateSignedToken(signingClient, [new Claim("feature_flags", "custom-branding")]);
        var postRefreshRefreshToken = CreateSignedToken(signingClient, []);
        RefreshStubAppGatewayApplicationFactory.SetStubResponse(postRefreshRefreshToken, postRefreshAccessToken);

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            Items =
            {
                [UserFeatureFlagsResponseTransform.CurrentAccessTokenItemKey] = preRefreshAccessToken,
                [UserFeatureFlagsResponseTransform.InboundRefreshTokenItemKey] = inboundRefreshToken
            }
        };
        context.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
        var transformContext = new ResponseTransformContext { HttpContext = context };

        await transform.ApplyAsync(transformContext);

        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be("custom-branding");
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().Contain(h => h!.Contains(AuthenticationTokenHttpKeys.RefreshTokenCookieName));
        setCookieHeaders.Should().Contain(h => h!.Contains(AuthenticationTokenHttpKeys.AccessTokenCookieName));
    }

    [Fact]
    public async Task ApplyAsync_WhenRefreshEndpointSignalsSessionRevoked_ShouldOverwriteResponseWith401AndClearCookies()
    {
        // The refresh endpoint can detect a revoked session (e.g. token replay) and return 401 with
        // x-unauthorized-reason. The transform must surface that to the SPA, not silently let the
        // upstream success bleed through with the original cookies still valid.
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var transform = stubFactory.Services.GetRequiredService<UserFeatureFlagsResponseTransform>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();

        var inboundRefreshToken = CreateSignedToken(signingClient, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, [new Claim("feature_flags", "stale-flag")]);
        RefreshStubAppGatewayApplicationFactory.SetStubRevoked("ReplayAttackDetected");

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream(), StatusCode = StatusCodes.Status200OK },
            Items =
            {
                [UserFeatureFlagsResponseTransform.CurrentAccessTokenItemKey] = preRefreshAccessToken,
                [UserFeatureFlagsResponseTransform.InboundRefreshTokenItemKey] = inboundRefreshToken
            }
        };
        context.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
        var transformContext = new ResponseTransformContext { HttpContext = context };

        await transform.ApplyAsync(transformContext);

        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers[AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey].ToString().Should().Be("ReplayAttackDetected");
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey);
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    private static string CreateSignedToken(ITokenSigningClient signingClient, Claim[] claims)
    {
        var now = DateTimeOffset.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = now.AddMinutes(5).UtcDateTime,
            Issuer = signingClient.Issuer,
            Audience = signingClient.Audience,
            SigningCredentials = signingClient.GetSigningCredentials(),
            Subject = new ClaimsIdentity(claims)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

internal sealed class RefreshStubAppGatewayApplicationFactory : WebApplicationFactory<Program>
{
    private static string _refreshToken = string.Empty;
    private static string _accessToken = string.Empty;
    private static string? _revokedReason;

    public static void SetStubResponse(string refreshToken, string accessToken)
    {
        _refreshToken = refreshToken;
        _accessToken = accessToken;
        _revokedReason = null;
    }

    public static void SetStubRevoked(string revokedReason)
    {
        _revokedReason = revokedReason;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.AddFilter(_ => false));
        builder.ConfigureServices(services => { services.AddHttpClient("Account").ConfigurePrimaryHttpMessageHandler(() => new StubRefreshHandler()); }
        );
    }

    private sealed class StubRefreshHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_revokedReason is not null)
            {
                var revoked = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                revoked.Headers.Add(AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, _revokedReason);
                return Task.FromResult(revoked);
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, _refreshToken);
            response.Headers.Add(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, _accessToken);
            return Task.FromResult(response);
        }
    }
}
