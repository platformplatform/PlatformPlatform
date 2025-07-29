using FluentAssertions;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Platform;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users.Domain;

public sealed class UserTests
{
    private readonly TenantId _tenantId = TenantId.NewId();

    [Fact]
    public void IsInternalUser_ShouldReturnTrueForInternalEmails()
    {
        // Arrange
        var internalEmails = new[]
        {
            $"user{Settings.Current.Identity.InternalEmailDomain}",
            $"admin{Settings.Current.Identity.InternalEmailDomain}",
            $"test.user{Settings.Current.Identity.InternalEmailDomain}",
            $"user+tag{Settings.Current.Identity.InternalEmailDomain}",
            $"USER{Settings.Current.Identity.InternalEmailDomain.ToUpperInvariant()}"
        };

        foreach (var email in internalEmails)
        {
            // Arrange
            var user = User.Create(_tenantId, email, UserRole.Member, true, "en-US");

            // Act
            var isInternal = user.IsInternalUser;

            // Assert
            isInternal.Should().BeTrue($"Email {email} should be identified as internal");
        }
    }

    [Fact]
    public void IsInternalUser_ShouldReturnFalseForExternalEmails()
    {
        // Arrange
        var externalEmails = new[]
        {
            "user@example.com",
            "user@company.net",
            $"{Settings.Current.Identity.InternalEmailDomain.Substring(1)}@example.com",
            $"user@subdomain.{Settings.Current.Identity.InternalEmailDomain.Substring(1)}"
        };

        foreach (var email in externalEmails)
        {
            // Arrange
            var user = User.Create(_tenantId, email, UserRole.Member, true, "en-US");

            // Act
            var isInternal = user.IsInternalUser;

            // Assert
            isInternal.Should().BeFalse($"Email {email} should be identified as external");
        }
    }
}
