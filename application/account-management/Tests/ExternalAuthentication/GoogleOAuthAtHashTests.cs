using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Google;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ExternalAuthentication;

public sealed class GoogleOAuthAtHashTests
{
    [Fact]
    public void ComputeAtHash_WhenRS256_ShouldReturnCorrectHash()
    {
        // Arrange
        var accessToken = "test-access-token-value";
        var expectedHash = ComputeExpectedAtHash(accessToken, SHA256.Create());

        // Act
        var result = GoogleOAuthProvider.ComputeAtHash(accessToken, "RS256");

        // Assert
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeAtHash_WhenRS384_ShouldReturnCorrectHash()
    {
        // Arrange
        var accessToken = "test-access-token-value";
        var expectedHash = ComputeExpectedAtHash(accessToken, SHA384.Create());

        // Act
        var result = GoogleOAuthProvider.ComputeAtHash(accessToken, "RS384");

        // Assert
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeAtHash_WhenRS512_ShouldReturnCorrectHash()
    {
        // Arrange
        var accessToken = "test-access-token-value";
        var expectedHash = ComputeExpectedAtHash(accessToken, SHA512.Create());

        // Act
        var result = GoogleOAuthProvider.ComputeAtHash(accessToken, "RS512");

        // Assert
        result.Should().Be(expectedHash);
    }

    [Fact]
    public void ComputeAtHash_WhenUnsupportedAlgorithm_ShouldReturnNull()
    {
        // Act
        var result = GoogleOAuthProvider.ComputeAtHash("test-token", "ES256");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ComputeAtHash_WhenKnownAccessToken_ShouldMatchExpectedValue()
    {
        // Arrange
        var accessToken = "ya29.a0AfH6SMBx";
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
        var leftHalf = hash[..(hash.Length / 2)];
        var expected = Base64UrlEncoder.Encode(leftHalf);

        // Act
        var result = GoogleOAuthProvider.ComputeAtHash(accessToken, "RS256");

        // Assert
        result.Should().Be(expected);
    }

    private static string ComputeExpectedAtHash(string accessToken, HashAlgorithm hashAlgorithm)
    {
        using (hashAlgorithm)
        {
            var hash = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
            var leftHalf = hash[..(hash.Length / 2)];
            return Base64UrlEncoder.Encode(leftHalf);
        }
    }
}
