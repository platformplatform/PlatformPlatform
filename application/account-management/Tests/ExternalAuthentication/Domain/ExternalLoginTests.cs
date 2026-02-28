using System.Security;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ExternalAuthentication.Domain;

public sealed class ExternalLoginTests
{
    [Fact]
    public void Create_WhenCalledWithValidParameters_ShouldSetAllPropertiesCorrectly()
    {
        // Act
        var externalLogin = ExternalLogin.Create(
            ExternalLoginType.Login,
            ExternalProviderType.Google,
            "code-verifier-123",
            "nonce-abc",
            "browser-fingerprint-abc"
        );

        // Assert
        externalLogin.Id.Should().NotBeNull();
        externalLogin.Type.Should().Be(ExternalLoginType.Login);
        externalLogin.ProviderType.Should().Be(ExternalProviderType.Google);
        externalLogin.Email.Should().BeNull();
        externalLogin.CodeVerifier.Should().Be("code-verifier-123");
        externalLogin.Nonce.Should().Be("nonce-abc");
        externalLogin.BrowserFingerprint.Should().Be("browser-fingerprint-abc");
        externalLogin.LoginResult.Should().BeNull();
        externalLogin.IsConsumed.Should().BeFalse();
    }

    [Fact]
    public void MarkCompleted_WhenNotYetConsumed_ShouldSetLoginResultToSuccessAndEmail()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();

        // Act
        externalLogin.MarkCompleted("user@example.com");

        // Assert
        externalLogin.Email.Should().Be("user@example.com");
        externalLogin.LoginResult.Should().Be(ExternalLoginResult.Success);
        externalLogin.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyCompleted_ShouldThrowUnreachableException()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        externalLogin.MarkCompleted("user@example.com");

        // Act
        var act = () => externalLogin.MarkCompleted("user@example.com");

        // Assert
        act.Should().Throw<UnreachableException>()
            .WithMessage("The external login has already been completed.");
    }

    [Fact]
    public void MarkCompleted_WhenAlreadyFailed_ShouldThrowUnreachableException()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        externalLogin.MarkFailed(ExternalLoginResult.CodeExchangeFailed);

        // Act
        var act = () => externalLogin.MarkCompleted("user@example.com");

        // Assert
        act.Should().Throw<UnreachableException>()
            .WithMessage("The external login has already been completed.");
    }

    [Fact]
    public void MarkFailed_WhenNotYetConsumed_ShouldSetLoginResultToFailureReason()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();

        // Act
        externalLogin.MarkFailed(ExternalLoginResult.CodeExchangeFailed);

        // Assert
        externalLogin.LoginResult.Should().Be(ExternalLoginResult.CodeExchangeFailed);
        externalLogin.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public void MarkFailed_WhenCalledWithSuccess_ShouldThrowUnreachableException()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();

        // Act
        var act = () => externalLogin.MarkFailed(ExternalLoginResult.Success);

        // Assert
        act.Should().Throw<UnreachableException>()
            .WithMessage("Cannot mark a login as failed with a success result.");
    }

    [Fact]
    public void MarkFailed_WhenAlreadyCompleted_ShouldThrowUnreachableException()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        externalLogin.MarkCompleted("user@example.com");

        // Act
        var act = () => externalLogin.MarkFailed(ExternalLoginResult.LoginExpired);

        // Assert
        act.Should().Throw<UnreachableException>()
            .WithMessage("The external login has already been completed.");
    }

    [Fact]
    public void MarkFailed_WhenAlreadyFailed_ShouldThrowUnreachableException()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        externalLogin.MarkFailed(ExternalLoginResult.InvalidState);

        // Act
        var act = () => externalLogin.MarkFailed(ExternalLoginResult.LoginExpired);

        // Assert
        act.Should().Throw<UnreachableException>()
            .WithMessage("The external login has already been completed.");
    }

    [Fact]
    public void IsExpired_WhenWithinValidPeriod_ShouldReturnFalse()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        var now = externalLogin.CreatedAt.AddSeconds(ExternalLogin.ValidForSeconds - 1);

        // Act
        var isExpired = externalLogin.IsExpired(now);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExactlyAtExpiry_ShouldReturnFalse()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        var now = externalLogin.CreatedAt.AddSeconds(ExternalLogin.ValidForSeconds);

        // Act
        var isExpired = externalLogin.IsExpired(now);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastValidPeriod_ShouldReturnTrue()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        var now = externalLogin.CreatedAt.AddSeconds(ExternalLogin.ValidForSeconds + 1);

        // Act
        var isExpired = externalLogin.IsExpired(now);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenNowIsBeforeCreatedAt_ShouldThrowSecurityException()
    {
        // Arrange
        var externalLogin = CreateExternalLogin();
        var pastTime = externalLogin.CreatedAt.AddSeconds(-1);

        // Act
        var act = () => externalLogin.IsExpired(pastTime);

        // Assert
        act.Should().Throw<SecurityException>()
            .WithMessage("*CreatedAt in the future*");
    }

    [Fact]
    public void AddExternalIdentity_WhenNewProvider_ShouldAddIdentity()
    {
        // Arrange
        var user = User.Create(TenantId.NewId(), "user@example.com", UserRole.Member, true, "en-US");

        // Act
        user.AddExternalIdentity(ExternalProviderType.Google, "google-user-id-123");

        // Assert
        user.ExternalIdentities.Should().HaveCount(1);
        user.ExternalIdentities[0].Provider.Should().Be(ExternalProviderType.Google);
        user.ExternalIdentities[0].ProviderUserId.Should().Be("google-user-id-123");
    }

    [Fact]
    public void AddExternalIdentity_WhenDuplicateProvider_ShouldThrowUnreachableException()
    {
        // Arrange
        var user = User.Create(TenantId.NewId(), "user@example.com", UserRole.Member, true, "en-US");
        user.AddExternalIdentity(ExternalProviderType.Google, "google-user-id-123");

        // Act
        var act = () => user.AddExternalIdentity(ExternalProviderType.Google, "different-google-id");

        // Assert
        act.Should().Throw<UnreachableException>()
            .WithMessage("*already has an external identity*");
    }

    [Fact]
    public void GetExternalIdentity_WhenProviderExists_ShouldReturnIdentity()
    {
        // Arrange
        var user = User.Create(TenantId.NewId(), "user@example.com", UserRole.Member, true, "en-US");
        user.AddExternalIdentity(ExternalProviderType.Google, "google-user-id-123");

        // Act
        var identity = user.GetExternalIdentity(ExternalProviderType.Google);

        // Assert
        identity.Should().NotBeNull();
        identity.Provider.Should().Be(ExternalProviderType.Google);
        identity.ProviderUserId.Should().Be("google-user-id-123");
    }

    [Fact]
    public void GetExternalIdentity_WhenProviderDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var user = User.Create(TenantId.NewId(), "user@example.com", UserRole.Member, true, "en-US");

        // Act
        var identity = user.GetExternalIdentity(ExternalProviderType.Google);

        // Assert
        identity.Should().BeNull();
    }

    private static ExternalLogin CreateExternalLogin()
    {
        return ExternalLogin.Create(
            ExternalLoginType.Login,
            ExternalProviderType.Google,
            "code-verifier",
            "nonce-value",
            "browser-fingerprint"
        );
    }
}
