using System.Net;
using System.Text;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class HandleStripeWebhookTests : EndpointBaseTest<AccountDbContext>
{
    private const string WebhookUrl = "/api/account/subscriptions/stripe-webhook";

    private string InsertSubscription(string? stripeCustomerId = MockStripeClient.MockCustomerId, string plan = nameof(SubscriptionPlan.Standard), DateTimeOffset? firstPaymentFailedAt = null, DateTimeOffset? lastNotificationSentAt = null, DateTimeOffset? disputedAt = null, DateTimeOffset? refundedAt = null)
    {
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", plan),
                ("ScheduledPlan", null),
                ("StripeCustomerId", stripeCustomerId),
                ("StripeSubscriptionId", MockStripeClient.MockSubscriptionId),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", firstPaymentFailedAt),
                ("LastNotificationSentAt", lastNotificationSentAt),
                ("DisputedAt", disputedAt),
                ("RefundedAt", refundedAt),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );
        return subscriptionId;
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenInvalidSignature_ShouldReturnBadRequest()
    {
        // Arrange
        InsertSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "invalid_signature");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Invalid webhook signature.");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenDuplicateEvent_ShouldReturnSuccess()
    {
        // Arrange
        InsertSubscription();
        var eventId = $"{MockStripeClient.MockWebhookEventId}_duplicate";
        Connection.Insert("StripeEvents", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", eventId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("ProcessedAt", TimeProvider.GetUtcNow()),
                ("EventType", "checkout.session.completed"),
                ("StripeCustomerId", MockStripeClient.MockCustomerId),
                ("StripeSubscriptionId", MockStripeClient.MockSubscriptionId),
                ("Payload", null)
            ]
        );
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"event_type:checkout.session.completed,event_id:{eventId}");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenCheckoutSessionCompleted_ShouldSyncSubscription()
    {
        // Arrange
        InsertSubscription(plan: nameof(SubscriptionPlan.Basis));
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenPaymentSucceeded_ShouldClearPaymentFailure()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        var subscriptionId = InsertSubscription(firstPaymentFailedAt: now.AddHours(-48), lastNotificationSentAt: now.AddHours(-24));
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.Value, [("State", nameof(TenantState.PastDue))]);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:invoice.payment_succeeded");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var firstPaymentFailed = Connection.ExecuteScalar<string>("SELECT FirstPaymentFailedAt FROM Subscriptions WHERE Id = @id", [new { id = subscriptionId }]);
        firstPaymentFailed.Should().BeNullOrEmpty();

        var tenantState = Connection.ExecuteScalar<string>("SELECT State FROM Tenants WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Active));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("PaymentRecovered");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenFirstPaymentFailed_ShouldTransitionToPastDue()
    {
        // Arrange
        InsertSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:invoice.payment_failed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var firstPaymentFailed = Connection.ExecuteScalar<string>("SELECT FirstPaymentFailedAt FROM Subscriptions WHERE Id = @id", [new { id = Connection.ExecuteScalar<string>("SELECT Id FROM Subscriptions WHERE StripeCustomerId = @customerId", [new { customerId = MockStripeClient.MockCustomerId }]) }]);
        firstPaymentFailed.Should().NotBeNullOrEmpty();

        var tenantState = Connection.ExecuteScalar<string>("SELECT State FROM Tenants WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.PastDue));

        await EmailClient.Received(1).SendAsync(
            Arg.Is<string>(e => e == "owner@tenant-1.com"),
            Arg.Is<string>(s => s.Contains("Payment failed")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("PaymentFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenPaymentFailedWithinGracePeriod_ShouldSendReminder()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        InsertSubscription(firstPaymentFailedAt: now.AddHours(-48), lastNotificationSentAt: now.AddHours(-25));
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:invoice.payment_failed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<string>(e => e == "owner@tenant-1.com"),
            Arg.Is<string>(s => s.Contains("Payment reminder")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenPaymentFailedWithinCooldown_ShouldNotSendReminder()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        InsertSubscription(firstPaymentFailedAt: now.AddHours(-48), lastNotificationSentAt: now.AddHours(-12));
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:invoice.payment_failed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        await EmailClient.DidNotReceive().SendAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenPaymentFailedAfterGracePeriod_ShouldSuspendSubscription()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        InsertSubscription(firstPaymentFailedAt: now.AddHours(-73), lastNotificationSentAt: now.AddHours(-25));
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.Value, [("State", nameof(TenantState.PastDue))]);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:invoice.payment_failed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantState = Connection.ExecuteScalar<string>("SELECT State FROM Tenants WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Suspended));

        await EmailClient.Received(1).SendAsync(
            Arg.Is<string>(e => e == "owner@tenant-1.com"),
            Arg.Is<string>(s => s.Contains("suspended")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionSuspended");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenSubscriptionDeleted_ShouldSuspendTenant()
    {
        // Arrange
        InsertSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.deleted");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantState = Connection.ExecuteScalar<string>("SELECT State FROM Tenants WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Suspended));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SubscriptionSuspended");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenNoSubscriptionFound_ShouldRecordEventAndReturnSuccess()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:checkout.session.completed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenCustomerSubscriptionUpdated_ShouldSyncState()
    {
        // Arrange
        InsertSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.updated");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenDisputeCreated_ShouldSetDisputedAndSendEmail()
    {
        // Arrange
        var subscriptionId = InsertSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:charge.dispute.created");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var disputedAt = Connection.ExecuteScalar<string>("SELECT DisputedAt FROM Subscriptions WHERE Id = @id", [new { id = subscriptionId }]);
        disputedAt.Should().NotBeNullOrEmpty();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<string>(e => e == "owner@tenant-1.com"),
            Arg.Is<string>(s => s.Contains("dispute")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("PaymentDisputed");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenDisputeClosed_ShouldClearDispute()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        var subscriptionId = InsertSubscription(disputedAt: now.AddDays(-5));
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:charge.dispute.closed");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var disputedAt = Connection.ExecuteScalar<string>("SELECT DisputedAt FROM Subscriptions WHERE Id = @id", [new { id = subscriptionId }]);
        disputedAt.Should().BeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("DisputeResolved");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task HandleStripeWebhook_WhenChargeRefunded_ShouldSetRefundedAt()
    {
        // Arrange
        var subscriptionId = InsertSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:charge.refunded");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var refundedAt = Connection.ExecuteScalar<string>("SELECT RefundedAt FROM Subscriptions WHERE Id = @id", [new { id = subscriptionId }]);
        refundedAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("PaymentRefunded");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("WebhookProcessed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
