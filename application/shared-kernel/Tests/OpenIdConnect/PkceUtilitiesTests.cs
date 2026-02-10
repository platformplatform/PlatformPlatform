using System.Text.RegularExpressions;
using FluentAssertions;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.OpenIdConnect;

public sealed partial class PkceUtilitiesTests
{
    [Fact]
    public void GenerateCodeVerifier_WhenCalled_ShouldReturn128Characters()
    {
        // Act
        var codeVerifier = PkceUtilities.GenerateCodeVerifier();

        // Assert
        codeVerifier.Length.Should().Be(128);
    }

    [Fact]
    public void GenerateCodeVerifier_WhenCalled_ShouldContainOnlyBase64UrlCharacters()
    {
        // Arrange
        var base64UrlRegex = Base64UrlPattern();

        // Act
        var codeVerifier = PkceUtilities.GenerateCodeVerifier();

        // Assert
        base64UrlRegex.IsMatch(codeVerifier).Should().BeTrue();
    }

    [Fact]
    public void GenerateCodeVerifier_WhenCalledMultipleTimes_ShouldReturnUniqueValues()
    {
        // Arrange
        const int count = 100;
        var verifiers = new HashSet<string>();

        // Act
        for (var i = 0; i < count; i++)
        {
            verifiers.Add(PkceUtilities.GenerateCodeVerifier());
        }

        // Assert
        verifiers.Count.Should().Be(count);
    }

    [Fact]
    public void GenerateCodeChallenge_WhenCalledWithVerifier_ShouldReturnBase64UrlEncodedHash()
    {
        // Arrange
        var codeVerifier = PkceUtilities.GenerateCodeVerifier();
        var base64UrlRegex = Base64UrlPattern();

        // Act
        var codeChallenge = PkceUtilities.GenerateCodeChallenge(codeVerifier);

        // Assert
        codeChallenge.Should().NotBeNullOrEmpty();
        base64UrlRegex.IsMatch(codeChallenge).Should().BeTrue();
    }

    [Fact]
    public void GenerateCodeChallenge_WhenCalledWithSameVerifier_ShouldReturnSameChallenge()
    {
        // Arrange
        var codeVerifier = PkceUtilities.GenerateCodeVerifier();

        // Act
        var challenge1 = PkceUtilities.GenerateCodeChallenge(codeVerifier);
        var challenge2 = PkceUtilities.GenerateCodeChallenge(codeVerifier);

        // Assert
        challenge1.Should().Be(challenge2);
    }

    [Fact]
    public void GenerateCodeChallenge_WhenCalledWithDifferentVerifiers_ShouldReturnDifferentChallenges()
    {
        // Arrange
        var verifier1 = PkceUtilities.GenerateCodeVerifier();
        var verifier2 = PkceUtilities.GenerateCodeVerifier();

        // Act
        var challenge1 = PkceUtilities.GenerateCodeChallenge(verifier1);
        var challenge2 = PkceUtilities.GenerateCodeChallenge(verifier2);

        // Assert
        challenge1.Should().NotBe(challenge2);
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex Base64UrlPattern();
}
