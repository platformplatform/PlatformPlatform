using System.Net;
using System.Text;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

[Collection("StripeTests")]
public sealed class AcknowledgeStripeWebhookTests : EndpointBaseTest<AccountDbContext>
{
    private const string WebhookUrl = "/api/account/subscriptions/stripe-webhook";

    protected override void Dispose(bool disposing)
    {
        MockStripeClient.ResetOverrides();
        base.Dispose(disposing);
    }

    private void SetupSubscription(string? stripeCustomerId = MockStripeClient.MockCustomerId, string? stripeSubscriptionId = MockStripeClient.MockSubscriptionId, string plan = nameof(SubscriptionPlan.Standard), DateTimeOffset? firstPaymentFailedAt = null, string? cancellationReason = null)
    {
        var hasStripeSubscription = stripeSubscriptionId is not null;
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("plan", plan),
                ("stripe_customer_id", stripeCustomerId),
                ("stripe_subscription_id", stripeSubscriptionId),
                ("current_price_amount", hasStripeSubscription ? 29.99m : null),
                ("current_price_currency", hasStripeSubscription ? "USD" : null),
                ("current_period_end", hasStripeSubscription ? TimeProvider.GetUtcNow().AddDays(30) : null),
                ("first_payment_failed_at", firstPaymentFailedAt),
                ("cancellation_reason", cancellationReason)
            ]
        );
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenInvalidSignature_ShouldReturnBadRequest()
    {
        // Arrange
        SetupSubscription();
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
    public async Task AcknowledgeStripeWebhook_WhenDuplicateEvent_ShouldReturnSuccess()
    {
        // Arrange
        SetupSubscription();
        var eventId = $"{MockStripeClient.MockWebhookEventId}_duplicate";
        Connection.Insert("stripe_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", eventId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("event_type", "checkout.session.completed"),
                ("status", nameof(StripeEventStatus.Processed)),
                ("processed_at", TimeProvider.GetUtcNow()),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("payload", null),
                ("error", null)
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
    public async Task AcknowledgeStripeWebhook_WhenCheckoutSessionCompleted_ShouldSyncSubscription()
    {
        // Arrange
        SetupSubscription(stripeSubscriptionId: null, plan: nameof(SubscriptionPlan.Basis));
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
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenPaymentSucceeded_ShouldClearPaymentFailure()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        SetupSubscription(firstPaymentFailedAt: now.AddHours(-48));
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

        var firstPaymentFailed = Connection.ExecuteScalar<string>("SELECT first_payment_failed_at FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]);
        firstPaymentFailed.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenFirstPaymentFailed_ShouldSetFailure()
    {
        // Arrange
        MockStripeClient.OverrideSubscriptionStatus = StripeSubscriptionStatus.PastDue;
        SetupSubscription();
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

        var firstPaymentFailed = Connection.ExecuteScalar<string>("SELECT first_payment_failed_at FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]);
        firstPaymentFailed.Should().NotBeNullOrEmpty();

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Active));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenSubsequentPaymentFailed_ShouldNotUpdateFailureTimestamp()
    {
        // Arrange
        MockStripeClient.OverrideSubscriptionStatus = StripeSubscriptionStatus.PastDue;
        var now = TimeProvider.GetUtcNow();
        SetupSubscription(firstPaymentFailedAt: now.AddHours(-48));
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

        var firstPaymentFailed = Connection.ExecuteScalar<string>("SELECT first_payment_failed_at FROM subscriptions WHERE tenant_id = @tenantId", [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]);
        firstPaymentFailed.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenSubscriptionDeletedInvoluntarily_ShouldSuspendTenant()
    {
        // Arrange
        MockStripeClient.SimulateSubscriptionDeleted = true;
        var now = TimeProvider.GetUtcNow();
        SetupSubscription(firstPaymentFailedAt: now.AddDays(-5));
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

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Suspended));

        var suspensionReason = Connection.ExecuteScalar<string>("SELECT suspension_reason FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        suspensionReason.Should().Be(nameof(SuspensionReason.PaymentFailed));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenSubscriptionDeletedVoluntarily_ShouldKeepTenantActive()
    {
        // Arrange
        MockStripeClient.SimulateSubscriptionDeleted = true;
        SetupSubscription(cancellationReason: nameof(CancellationReason.NoLongerNeeded));
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

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Active));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenCheckoutSessionCompleted_ShouldActivateSuspendedTenant()
    {
        // Arrange
        SetupSubscription(stripeSubscriptionId: null, plan: nameof(SubscriptionPlan.Basis));
        Connection.Update("tenants", "id", DatabaseSeeder.Tenant1.Id.Value, [("state", nameof(TenantState.Suspended)), ("suspension_reason", nameof(SuspensionReason.PaymentFailed))]);
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

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Active));

        var suspensionReason = Connection.ExecuteScalar<string>("SELECT suspension_reason FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        suspensionReason.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenCustomerDeleted_ShouldSuspendTenant()
    {
        // Arrange
        MockStripeClient.SimulateCustomerDeleted = true;
        SetupSubscription();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.deleted");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Suspended));

        var suspensionReason = Connection.ExecuteScalar<string>("SELECT suspension_reason FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        suspensionReason.Should().Be(nameof(SuspensionReason.CustomerDeleted));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenSubscriptionDeletedAndTenantAlreadySuspended_ShouldNotOverrideSuspension()
    {
        // Arrange - tenant already suspended with CustomerDeleted (e.g., customer.deleted processed in previous batch)
        MockStripeClient.SimulateSubscriptionDeleted = true;
        SetupSubscription(cancellationReason: nameof(CancellationReason.NoLongerNeeded));
        Connection.Update("tenants", "id", DatabaseSeeder.Tenant1.Id.Value, [("state", nameof(TenantState.Suspended)), ("suspension_reason", nameof(SuspensionReason.CustomerDeleted))]);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.deleted");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert - tenant should remain Suspended with CustomerDeleted, not overridden to Active or PaymentFailed
        response.EnsureSuccessStatusCode();

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Suspended));

        var suspensionReason = Connection.ExecuteScalar<string>("SELECT suspension_reason FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        suspensionReason.Should().Be(nameof(SuspensionReason.CustomerDeleted));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenCustomerDeletedAndSubscriptionDeletedInSameBatch_ShouldSuspendWithCustomerDeleted()
    {
        // Arrange - pre-insert a pending customer.deleted event so both events process in the same batch
        MockStripeClient.SimulateCustomerDeleted = true;
        SetupSubscription(cancellationReason: nameof(CancellationReason.NoLongerNeeded));
        Connection.Insert("stripe_events", [
                ("tenant_id", null),
                ("id", $"{MockStripeClient.MockWebhookEventId}_customer_deleted"),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("event_type", "customer.deleted"),
                ("status", nameof(StripeEventStatus.Pending)),
                ("processed_at", null),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", null),
                ("payload", null),
                ("error", null)
            ]
        );
        TelemetryEventsCollectorSpy.Reset();

        // Act - send subscription.deleted webhook, which triggers processing of both pending events
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent($"customer:{MockStripeClient.MockCustomerId}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:customer.subscription.deleted");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert - customer.deleted should take priority, tenant suspended with CustomerDeleted
        response.EnsureSuccessStatusCode();

        var tenantState = Connection.ExecuteScalar<string>("SELECT state FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        tenantState.Should().Be(nameof(TenantState.Suspended));

        var suspensionReason = Connection.ExecuteScalar<string>("SELECT suspension_reason FROM tenants WHERE id = @id", [new { id = DatabaseSeeder.Tenant1.Id.Value }]);
        suspensionReason.Should().Be(nameof(SuspensionReason.CustomerDeleted));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenNoSubscriptionFound_ShouldStoreEventAndReturnSuccess()
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

        var eventCount = Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM stripe_events WHERE stripe_customer_id = @customerId", [new { customerId = MockStripeClient.MockCustomerId }]);
        eventCount.Should().Be(1);

        var eventStatus = Connection.ExecuteScalar<string>("SELECT status FROM stripe_events WHERE stripe_customer_id = @customerId", [new { customerId = MockStripeClient.MockCustomerId }]);
        eventStatus.Should().Be(nameof(StripeEventStatus.Pending));
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenCustomerSubscriptionUpdated_ShouldSyncState()
    {
        // Arrange
        SetupSubscription();
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
    }

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenNoCustomerId_ShouldStoreAsIgnored()
    {
        // Act
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent("no_customer", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "event_type:product.created");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
