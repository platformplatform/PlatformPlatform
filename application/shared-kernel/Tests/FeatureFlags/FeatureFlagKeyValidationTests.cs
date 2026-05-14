using FluentAssertions;
using Xunit;
using FeatureFlagRegistry = SharedKernel.FeatureFlags.FeatureFlags;

namespace SharedKernel.Tests.FeatureFlags;

public sealed class FeatureFlagKeyValidationTests
{
    [Theory]
    [InlineData("sso")]
    [InlineData("google-oauth")]
    [InlineData("account-overview")]
    [InlineData("subscriptions")]
    [InlineData("compact-view")]
    [InlineData("experimental-ui")]
    [InlineData("a")]
    [InlineData("a1")]
    [InlineData("1-a")]
    [InlineData("plan-tier-2")]
    public void IsValidKey_WhenKeyIsLowercaseKebabCase_ShouldReturnTrue(string key)
    {
        FeatureFlagRegistry.IsValidKey(key).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--hyphen")]
    [InlineData("UPPER")]
    [InlineData("Mixed-Case")]
    [InlineData("with_underscore")]
    [InlineData("with space")]
    [InlineData("with.dot")]
    [InlineData("with,comma")]
    [InlineData("with/slash")]
    [InlineData("with:colon")]
    [InlineData("emoji-😀")]
    public void IsValidKey_WhenKeyViolatesKebabCase_ShouldReturnFalse(string key)
    {
        FeatureFlagRegistry.IsValidKey(key).Should().BeFalse();
    }
}
