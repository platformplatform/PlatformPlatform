using System.Net;
using System.Security.Claims;
using AppGateway.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenSigning;
using Xunit;

namespace AppGateway.Tests;

public sealed class AuthenticationCookiePathTests(AppGatewayApplicationFactory factory) : IClassFixture<AppGatewayApplicationFactory>
{
    [Fact]
    public async Task InvokeAsync_WhenUpstreamResponseCarriesTokenHeaders_ShouldIssueHostCookiesWithPathSlash()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = scope.ServiceProvider.GetRequiredService<ITokenSigningClient>();
        var refreshToken = CreateSignedToken(signingClient, 60, []);
        var accessToken = CreateSignedToken(signingClient, 5, []);
        var context = CreateHttpContext("/api/account/authentication/switch-tenant");

        // Act
        await middleware.InvokeAsync(context, downstream =>
            {
                downstream.Response.Headers[AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey] = refreshToken;
                downstream.Response.Headers[AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey] = accessToken;
                return Task.CompletedTask;
            }
        );
        await TriggerOnStartingAsync(context);

        // Assert
        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().Contain(h => h!.Contains(AuthenticationTokenHttpKeys.RefreshTokenCookieName));
        setCookieHeaders.Should().Contain(h => h!.Contains(AuthenticationTokenHttpKeys.AccessTokenCookieName));
        foreach (var setCookie in setCookieHeaders.Where(h => h!.Contains("__Host-")))
        {
            setCookie.Should().Contain("path=/", $"every __Host- cookie must declare Path=/, but got: {setCookie}");
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenRefreshTokenCookieHasExpired_ShouldDeleteCookiesWithPathSlash()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = scope.ServiceProvider.GetRequiredService<ITokenSigningClient>();
        var expiredRefreshToken = CreateSignedToken(signingClient, -10, []);
        var expiredAccessToken = CreateSignedToken(signingClient, -10, []);
        var context = CreateHttpContext("/some-spa-path");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={expiredRefreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={expiredAccessToken}";

        // Act
        await middleware.InvokeAsync(context, _ => Task.CompletedTask);
        await TriggerOnStartingAsync(context);

        // Assert
        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().NotBeEmpty("expired cookies must be deleted");
        foreach (var setCookie in setCookieHeaders.Where(h => h!.Contains("__Host-")))
        {
            setCookie.Should().Contain("path=/", $"every __Host- cookie deletion must declare Path=/, but got: {setCookie}");
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenAuthenticatedRequestHasFeatureFlagsClaim_ShouldEmitUserFeatureFlagsHeader()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = scope.ServiceProvider.GetRequiredService<ITokenSigningClient>();
        var refreshToken = CreateSignedToken(signingClient, 60, []);
        var accessToken = CreateSignedToken(signingClient, 5, [new Claim("feature_flags", "account-overview,compact-view")]);
        var context = CreateHttpContext("/api/account/me");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={refreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={accessToken}";

        // Act
        await middleware.InvokeAsync(context, _ => Task.CompletedTask);
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be("account-overview,compact-view");
    }

    [Fact]
    public async Task InvokeAsync_WhenUpstreamSetsRefreshAuthenticationTokensHeader_ShouldEmitHeaderReflectingPostRefreshJwt()
    {
        // Arrange
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var middleware = stubFactory.Services.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();
        var inboundRefreshToken = CreateSignedToken(signingClient, 60, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim("feature_flags", "stale-flag")]);
        var postRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim("feature_flags", "account-overview")]);
        var postRefreshRefreshToken = CreateSignedToken(signingClient, 60, []);
        RefreshStubAppGatewayApplicationFactory.SetStubResponse(postRefreshRefreshToken, postRefreshAccessToken);
        var context = CreateHttpContext("/api/account/feature-flags/account-overview/tenant-override");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={inboundRefreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={preRefreshAccessToken}";

        // Act
        await middleware.InvokeAsync(context, downstream =>
            {
                downstream.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
                return Task.CompletedTask;
            }
        );
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be("account-overview");
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
        var setCookieHeaders = context.Response.Headers.SetCookie.ToArray();
        setCookieHeaders.Should().Contain(h => h!.Contains(AuthenticationTokenHttpKeys.RefreshTokenCookieName));
        setCookieHeaders.Should().Contain(h => h!.Contains(AuthenticationTokenHttpKeys.AccessTokenCookieName));
    }

    [Fact]
    public async Task InvokeAsync_WhenRefreshEndpointSignalsSessionRevoked_ShouldOverwriteResponseWith401AndClearCookies()
    {
        // Arrange
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var middleware = stubFactory.Services.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();
        var inboundRefreshToken = CreateSignedToken(signingClient, 60, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim("feature_flags", "stale-flag")]);
        RefreshStubAppGatewayApplicationFactory.SetStubRevoked("ReplayAttackDetected");
        var context = CreateHttpContext("/api/account/feature-flags/account-overview/tenant-override");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={inboundRefreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={preRefreshAccessToken}";

        // Act
        await middleware.InvokeAsync(context, downstream =>
            {
                downstream.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
                return Task.CompletedTask;
            }
        );
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.Headers[AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey].ToString().Should().Be("ReplayAttackDetected");
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey);
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext { Request = { Path = path }, Response = { Body = new MemoryStream() } };
        context.Features.Set<IHttpResponseFeature>(new CapturingResponseFeature());
        return context;
    }

    private static Task TriggerOnStartingAsync(HttpContext context)
    {
        var feature = (CapturingResponseFeature)context.Features.GetRequiredFeature<IHttpResponseFeature>();
        return feature.TriggerOnStartingAsync();
    }

    private static string CreateSignedToken(ITokenSigningClient signingClient, int validForMinutes, Claim[] claims)
    {
        var now = DateTimeOffset.UtcNow;
        var notBefore = validForMinutes >= 0 ? now.UtcDateTime : now.AddMinutes(validForMinutes - 1).UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = now.AddMinutes(validForMinutes).UtcDateTime,
            Issuer = signingClient.Issuer,
            Audience = signingClient.Audience,
            SigningCredentials = signingClient.GetSigningCredentials(),
            Subject = claims.Length == 0 ? null : new ClaimsIdentity(claims)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    ///     DefaultHttpContext's stock HttpResponseFeature treats OnStarting callbacks as no-ops because
    ///     the response is never actually started in a unit test. This replacement captures the
    ///     callbacks so the test can flush them after the downstream pipeline has set its response.
    /// </summary>
    private sealed class CapturingResponseFeature : HttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = [];

        public override void OnStarting(Func<object, Task> callback, object state)
        {
            _onStartingCallbacks.Add((callback, state));
        }

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public async Task TriggerOnStartingAsync()
        {
            foreach (var (callback, state) in _onStartingCallbacks)
            {
                await callback(state);
            }
        }
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
