using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Subscriptions.Queries;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Subscriptions;

public sealed class GetCheckoutPreviewTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetCheckoutPreview_WhenStandardPlan_ShouldReturnPreview()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", "cus_test_123")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/checkout-preview?Plan=Standard");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<CheckoutPreviewResponse>();
        result!.TotalAmount.Should().BeGreaterThan(0);
        result.Currency.Should().NotBeNullOrEmpty();
        result.TaxAmount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenBasisPlan_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/checkout-preview?Plan=Basis");

        // Assert
        var expectedErrors = new[] { new ErrorDetail("plan", "Cannot preview checkout for the Basis plan.") };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenNonOwner_ShouldReturnForbidden()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", "cus_test_123")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/subscriptions/checkout-preview?Plan=Standard");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can manage subscriptions.");
    }

    [Fact]
    public async Task GetCheckoutPreview_WhenNoStripeCustomer_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/subscriptions/checkout-preview?Plan=Standard");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Billing information must be saved before previewing checkout.");
    }
}
