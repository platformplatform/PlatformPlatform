using System.Globalization;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

/// <summary>
///     Hot-path integration tests for <see cref="ProcessPendingStripeEvents" /> covering the seeded-state
///     emission scenario. The webhook-driven sync seeds <see cref="StripeEventReplayer.ReplayState" /> from
///     the latest persisted <see cref="BillingEvent" />; without that seed an events.list anchor that has
///     aged past Stripe's 30-day window would replay against phantom-zero defaults and silently rewrite MRR.
/// </summary>
public sealed class ProcessPendingStripeEventsTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ExecuteAsync_WhenAnchorAgesOutOfEventsListWindow_ReplaysWithSeededStateNotPhantomZero()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("current_price_amount", 299m),
                ("current_price_currency", "DKK"),
                ("current_period_end", TimeProvider.GetUtcNow().AddDays(30)),
                ("last_synced_stripe_event_created_at", TimeProvider.GetUtcNow().AddDays(-31))
            ]
        );

        var subscriptionId = Connection.ExecuteScalar<string>(
            "SELECT id FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        var seededOccurredAt = TimeProvider.GetUtcNow().AddDays(-60);
        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", subscriptionId),
                ("created_at", seededOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", $"evt_seed_{Guid.NewGuid():N}"),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Standard)),
                ("previous_amount", 0m),
                ("new_amount", 299m),
                ("amount_delta", 299m),
                ("committed_mrr", 299m),
                ("currency", "DKK"),
                ("occurred_at", seededOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        // events.list mock returns only the post-anchor cancel toggle. The matching SubscriptionCreated has
        // aged out of Stripe's 30-day window, so the hot path can only see the cancel diff. Without seeding,
        // the resulting BillingEvent row would carry previousAmount=0 / amountDelta=0 / committedMrr=0.
        var cancelOccurredAt = TimeProvider.GetUtcNow().AddMinutes(-10);
        var cancelEventId = $"evt_cancel_after_anchor_{Guid.NewGuid():N}";
        var cancelPayload = """{"data":{"object":{"cancel_at_period_end":true,"currency":"dkk","items":{"data":[{"price":{"id":"price_mock_standard","unit_amount":29900,"currency":"dkk"}}]}},"previous_attributes":{"cancel_at_period_end":false}}}""";
        StripeState.EventsListAdditionalEvents.Add(new StripeReplayEvent(
                cancelEventId,
                "customer.subscription.updated",
                cancelOccurredAt,
                cancelPayload,
                MockStripeClient.MockApiVersion
            )
        );

        using var scope = WebApplicationServices.CreateScope();
        SetUseMockStripeCookieOnAmbientHttpContext(scope.ServiceProvider);
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();
        var stripeCustomerId = StripeCustomerId.NewId(MockStripeClient.MockCustomerId);

        // Act
        await processor.ExecuteAsync(stripeCustomerId, true, SyncMode.Apply, CancellationToken.None);

        // Assert
        var eventType = Connection.ExecuteScalar<string>(
            "SELECT event_type FROM billing_events WHERE stripe_event_id = @stripeEventId",
            [new { stripeEventId = cancelEventId }]
        );
        eventType.Should().Be(nameof(BillingEventType.SubscriptionCancelled), "cancel-toggle from events.list must classify as SubscriptionCancelled");

        var previousAmount = ReadDecimalColumn("previous_amount", cancelEventId);
        var amountDelta = ReadDecimalColumn("amount_delta", cancelEventId);
        var committedMrr = ReadDecimalColumn("committed_mrr", cancelEventId);
        previousAmount.Should().Be(299m, "previousAmount must derive from the seeded SubscriptionCreated PlanPrice — not phantom zero");
        amountDelta.Should().Be(-299m, "amountDelta must reflect MRR loss equal to the seeded committed_mrr");
        committedMrr.Should().Be(0m, "committedMrr after cancellation must drop to zero");
    }

    private decimal? ReadDecimalColumn(string columnName, string stripeEventId)
    {
        // EF Core maps decimal to TEXT in SQLite to preserve precision; the test helper's direct cast to
        // decimal therefore fails — read the raw string and parse with InvariantCulture.
        var raw = Connection.ExecuteScalar<string?>(
            $"SELECT {columnName} FROM billing_events WHERE stripe_event_id = @stripeEventId",
            [new { stripeEventId }]
        );
        return raw is null ? null : decimal.Parse(raw, CultureInfo.InvariantCulture);
    }

    private static void SetUseMockStripeCookieOnAmbientHttpContext(IServiceProvider serviceProvider)
    {
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        httpContextAccessor.HttpContext = httpContext;
    }
}
