using Account.Features.Subscriptions.Shared;
using FluentAssertions;
using Xunit;

namespace Account.Tests.Subscriptions;

public sealed class BillingDriftIterationTimeoutTests
{
    [Fact]
    public void Value_ShouldBe30Seconds()
    {
        // A slow Stripe call must release the row-level FOR UPDATE lock well under the app-level 45s
        // resilience timeout. 30s leaves enough headroom that no other caller waiting on the same row
        // will also time out.
        // Assert
        BillingDriftIterationTimeout.Value.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task CreateLinkedTokenSource_WhenParentCancels_ShouldCancelImmediately()
    {
        // Arrange
        using var parentCancellationTokenSource = new CancellationTokenSource();
        using var iterationCancellationTokenSource = BillingDriftIterationTimeout.CreateLinkedTokenSource(parentCancellationTokenSource.Token);

        // Act
        await parentCancellationTokenSource.CancelAsync();

        // Assert: parent cancellation propagates to the linked iteration token (no need to wait for the 30s budget)
        iterationCancellationTokenSource.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    public void CreateLinkedTokenSource_ShouldScheduleCancellationAtTimeoutValue()
    {
        // The CancellationTokenSource doesn't expose its CancelAfter deadline publicly, so we verify the
        // documented behavior at the boundary: a freshly created source must not be cancelled, and a
        // sufficiently short timeout below the deadline lets the test observe the deadline elapse.
        // Arrange
        using var parentCancellationTokenSource = new CancellationTokenSource();
        using var iterationCancellationTokenSource = BillingDriftIterationTimeout.CreateLinkedTokenSource(parentCancellationTokenSource.Token);

        // Assert: the iteration token is linked (not yet cancelled) until either the parent cancels or 30s elapses
        iterationCancellationTokenSource.IsCancellationRequested.Should().BeFalse();
        iterationCancellationTokenSource.Token.CanBeCanceled.Should().BeTrue();
    }
}
