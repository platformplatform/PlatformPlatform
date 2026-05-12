using System.Collections.Immutable;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using Xunit;

namespace Account.Tests.Subscriptions.Domain;

public sealed class SubscriptionConfigurationTests
{
    [Fact]
    public void DriftDiscrepancies_WhenSerializedWithDefaultOptions_ShouldEmitEnumsAsStrings()
    {
        // mirrors the production serialization path used by SubscriptionConfiguration
        // for the DriftDiscrepancies jsonb column. Pins the wire format so a future enum reorder
        // cannot silently remap historical rows persisted as integers.
        // Arrange
        var discrepancies = ImmutableArray.Create(new DriftDiscrepancy(
                DriftDiscrepancyKind.SubscriptionStateMismatch,
                "Plan mismatch between Stripe and local subscription.",
                DriftSeverity.Critical
            )
        );

        // Act
        var json = JsonSerializer.Serialize(discrepancies.ToArray(), JsonSerializerOptions.Default);

        // Assert
        json.Should().Contain("\"Kind\":\"SubscriptionStateMismatch\"");
        json.Should().Contain("\"Severity\":\"Critical\"");
    }
}
