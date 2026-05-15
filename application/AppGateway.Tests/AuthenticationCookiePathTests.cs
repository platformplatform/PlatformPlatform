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
        var accessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, "account-overview,compact-view")]);
        var context = CreateHttpContext("/api/account/me");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={refreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={accessToken}";

        // Act
        await middleware.InvokeAsync(context, _ => Task.CompletedTask);
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be("account-overview,compact-view");
    }

    [Fact]
    public async Task InvokeAsync_WhenAccessTokenHasNoFeatureFlagsClaim_ShouldOmitUserFeatureFlagsHeader()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = scope.ServiceProvider.GetRequiredService<ITokenSigningClient>();
        var refreshToken = CreateSignedToken(signingClient, 60, []);
        var accessToken = CreateSignedToken(signingClient, 5, []);
        var context = CreateHttpContext("/api/account/me");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={refreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={accessToken}";

        // Act
        await middleware.InvokeAsync(context, _ => Task.CompletedTask);
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey);
    }

    [Fact]
    public async Task InvokeAsync_WhenAccessTokenHasEmptyFeatureFlagsClaim_ShouldEmitEmptyUserFeatureFlagsHeader()
    {
        // Arrange
        await using var scope = factory.Services.CreateAsyncScope();
        var middleware = scope.ServiceProvider.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = scope.ServiceProvider.GetRequiredService<ITokenSigningClient>();
        var refreshToken = CreateSignedToken(signingClient, 60, []);
        var accessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, string.Empty)]);
        var context = CreateHttpContext("/api/account/me");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={refreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={accessToken}";

        // Act
        await middleware.InvokeAsync(context, _ => Task.CompletedTask);
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey(AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey);
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WhenUpstreamSetsRefreshAuthenticationTokensHeader_ShouldEmitHeaderReflectingPostRefreshJwt()
    {
        // Arrange
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var middleware = stubFactory.Services.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();
        var inboundRefreshToken = CreateSignedToken(signingClient, 60, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, "stale-flag")]);
        var postRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, "account-overview")]);
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
    public async Task InvokeAsync_WhenInlineRefreshPrecedesEndpointTriggeredRefresh_ShouldUseRotatedRefreshTokenForSecondCall()
    {
        // Arrange - expired access token forces inline refresh on the inbound path. The downstream
        // endpoint then signals x-refresh-authentication-tokens-required, triggering a second refresh.
        // Without M8 the second call would reuse the v=1 cookie value and fall back on the 30-second
        // grace window; with M8 the second call uses the v=2 token returned from the first refresh.
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var middleware = stubFactory.Services.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();
        var inboundRefreshTokenV1 = CreateSignedToken(signingClient, 60, []);
        var expiredAccessToken = CreateSignedToken(signingClient, -1, []);
        var rotatedRefreshTokenV2 = CreateSignedToken(signingClient, 60, []);
        var inlineRefreshedAccessToken = CreateSignedToken(signingClient, 5, []);
        var finalRefreshToken = CreateSignedToken(signingClient, 60, []);
        var finalAccessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, "account-overview")]);
        RefreshStubAppGatewayApplicationFactory.SetStubResponse(rotatedRefreshTokenV2, inlineRefreshedAccessToken);
        RefreshStubAppGatewayApplicationFactory.EnqueueStubResponse(finalRefreshToken, finalAccessToken);
        var context = CreateHttpContext("/api/account/me/change-locale");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={inboundRefreshTokenV1}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={expiredAccessToken}";

        // Act
        await middleware.InvokeAsync(context, downstream =>
            {
                downstream.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
                return Task.CompletedTask;
            }
        );
        await TriggerOnStartingAsync(context);

        // Assert
        var bearerTokens = RefreshStubAppGatewayApplicationFactory.ReceivedBearerTokens;
        bearerTokens.Should().HaveCount(2, "both an inline refresh and an endpoint-triggered refresh fired");
        bearerTokens[0].Should().Be(inboundRefreshTokenV1, "the inline refresh uses the cookie's v=1 token");
        bearerTokens[1].Should().Be(rotatedRefreshTokenV2, "the endpoint-triggered refresh must use the v=2 token returned from the inline refresh, not the stale v=1");
        context.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey].ToString().Should().Be("account-overview");
    }

    [Fact]
    public async Task InvokeAsync_WhenEndpointTriggeredRefreshHitsBackendOutage_ShouldOmitUserFeatureFlagsHeader()
    {
        // Arrange - downstream PUT succeeds and signals endpoint-triggered refresh. The refresh call to
        // the account backend throws HttpRequestException (simulating a transient outage). The middleware
        // logs and lets the mutation response through, but must NOT emit x-user-feature-flags from the
        // pre-refresh access token — that would mislead the SPA into "your toggle didn't take effect".
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var middleware = stubFactory.Services.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();
        var inboundRefreshToken = CreateSignedToken(signingClient, 60, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, "stale-flag")]);
        RefreshStubAppGatewayApplicationFactory.SetStubBackendUnavailable();
        var context = CreateHttpContext("/api/account/feature-flags/stale-flag/tenant-override");
        context.Request.Headers.Cookie = $"{AuthenticationTokenHttpKeys.RefreshTokenCookieName}={inboundRefreshToken}; {AuthenticationTokenHttpKeys.AccessTokenCookieName}={preRefreshAccessToken}";

        // Act
        await middleware.InvokeAsync(context, downstream =>
            {
                downstream.Response.StatusCode = StatusCodes.Status204NoContent;
                downstream.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
                return Task.CompletedTask;
            }
        );
        await TriggerOnStartingAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent, "the mutation response must pass through unchanged");
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey, "emitting the pre-refresh claim would tell the SPA the mutation had no effect");
        context.Response.Headers.Should().NotContainKey(AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey);
    }

    [Fact]
    public async Task InvokeAsync_WhenRefreshEndpointSignalsSessionRevoked_ShouldOverwriteResponseWith401AndClearCookies()
    {
        // Arrange
        await using var stubFactory = new RefreshStubAppGatewayApplicationFactory();
        var middleware = stubFactory.Services.GetRequiredService<AuthenticationCookieMiddleware>();
        var signingClient = stubFactory.Services.GetRequiredService<ITokenSigningClient>();
        var inboundRefreshToken = CreateSignedToken(signingClient, 60, []);
        var preRefreshAccessToken = CreateSignedToken(signingClient, 5, [new Claim(AuthenticationTokenHttpKeys.FeatureFlagsClaimName, "stale-flag")]);
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
    private static readonly Queue<(string Refresh, string Access)> ResponseQueue = new();
    private static readonly List<string?> ReceivedBearerTokensField = [];
    private static string? _revokedReason;
    private static bool _backendUnavailable;

    public static IReadOnlyList<string?> ReceivedBearerTokens => ReceivedBearerTokensField;

    public static void SetStubResponse(string refreshToken, string accessToken)
    {
        ResetStub();
        ResponseQueue.Enqueue((refreshToken, accessToken));
    }

    public static void EnqueueStubResponse(string refreshToken, string accessToken)
    {
        ResponseQueue.Enqueue((refreshToken, accessToken));
    }

    public static void SetStubRevoked(string revokedReason)
    {
        ResetStub();
        _revokedReason = revokedReason;
    }

    public static void SetStubBackendUnavailable()
    {
        ResetStub();
        _backendUnavailable = true;
    }

    private static void ResetStub()
    {
        ResponseQueue.Clear();
        ReceivedBearerTokensField.Clear();
        _revokedReason = null;
        _backendUnavailable = false;
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
            ReceivedBearerTokensField.Add(request.Headers.Authorization?.Parameter);

            if (_backendUnavailable)
            {
                throw new HttpRequestException("Stub backend unavailable.");
            }

            if (_revokedReason is not null)
            {
                var revoked = new HttpResponseMessage(HttpStatusCode.Unauthorized);
                revoked.Headers.Add(AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, _revokedReason);
                return Task.FromResult(revoked);
            }

            var (refreshToken, accessToken) = ResponseQueue.Dequeue();
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Headers.Add(AuthenticationTokenHttpKeys.RefreshTokenHttpHeaderKey, refreshToken);
            response.Headers.Add(AuthenticationTokenHttpKeys.AccessTokenHttpHeaderKey, accessToken);
            return Task.FromResult(response);
        }
    }
}
