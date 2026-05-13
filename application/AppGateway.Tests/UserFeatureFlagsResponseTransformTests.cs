using System.Security.Claims;
using AppGateway.Transformations;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
