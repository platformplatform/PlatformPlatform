using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Subscriptions;

public sealed class GetUpgradePreviewTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetUpgradePreview_WhenStandardToPremium_ShouldReturnPreview()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/upgrade-preview?NewPlan=Premium");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<UpgradePreviewResponse>();
        result!.TotalAmount.Should().BeGreaterThan(0);
        result.Currency.Should().NotBeNullOrEmpty();
        result.LineItems.Should().NotBeEmpty();
        result.LineItems.Sum(i => i.Amount).Should().Be(result.TotalAmount);
        var taxItem = result.LineItems.Should().ContainSingle(i => i.Description == "Tax").Which;
        taxItem.Amount.Should().BeGreaterThan(0);
        taxItem.IsProration.Should().BeFalse();
        result.LineItems.Where(i => i.IsProration).Should().AllSatisfy(item =>
            {
                item.Description.Should().NotBeNullOrEmpty();
                item.Currency.Should().NotBeNullOrEmpty();
            }
        );
    }

    [Fact]
    public async Task GetUpgradePreview_WhenPlanNotHigher_ShouldReturnBadRequest()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Premium)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/upgrade-preview?NewPlan=Standard");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Cannot upgrade from 'Premium' to 'Standard'. Target plan must be higher.");
    }

    [Fact]
    public async Task GetUpgradePreview_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", "sub_test_123"),
                ("CurrentPeriodEnd", TimeProvider.GetUtcNow().AddDays(30)),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/subscriptions/upgrade-preview?NewPlan=Premium");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");
    }

    [Fact]
    public async Task GetUpgradePreview_WhenNoStripeSubscription_ShouldReturnBadRequest()
    {
        // Arrange
        var subscriptionId = SubscriptionId.NewId().ToString();
        Connection.Insert("Subscriptions", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.Value),
                ("Id", subscriptionId),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Plan", nameof(SubscriptionPlan.Standard)),
                ("ScheduledPlan", null),
                ("StripeCustomerId", "cus_test_123"),
                ("StripeSubscriptionId", null),
                ("CurrentPeriodEnd", null),
                ("CancelAtPeriodEnd", false),
                ("FirstPaymentFailedAt", null),
                ("LastNotificationSentAt", null),
                ("PaymentTransactions", "[]"),
                ("PaymentMethod", null)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/upgrade-preview?NewPlan=Premium");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "No active Stripe subscription found.");
    }
}
