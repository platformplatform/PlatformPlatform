using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Shared;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetUpgradePreviewQuery(SubscriptionPlan NewPlan) : IRequest<Result<UpgradePreviewResponse>>;

[PublicAPI]
public sealed record UpgradePreviewResponse(decimal TotalAmount, string Currency, UpgradePreviewLineItemResponse[] LineItems);

[PublicAPI]
public sealed record UpgradePreviewLineItemResponse(string Description, decimal Amount, string Currency, bool IsProration);

public sealed class GetUpgradePreviewHandler(ISubscriptionRepository subscriptionRepository, StripeClientFactory stripeClientFactory, IExecutionContext executionContext)
    : IRequestHandler<GetUpgradePreviewQuery, Result<UpgradePreviewResponse>>
{
    public async Task<Result<UpgradePreviewResponse>> Handle(GetUpgradePreviewQuery query, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<UpgradePreviewResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        if (subscription.StripeSubscriptionId is null)
        {
            return Result<UpgradePreviewResponse>.BadRequest("No active Stripe subscription found.");
        }

        if (!query.NewPlan.IsUpgradeFrom(subscription.Plan))
        {
            return Result<UpgradePreviewResponse>.BadRequest($"Cannot upgrade from '{subscription.Plan}' to '{query.NewPlan}'. Target plan must be higher.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var preview = await stripeClient.GetUpgradePreviewAsync(subscription.StripeSubscriptionId, query.NewPlan, cancellationToken);
        if (preview is null)
        {
            return Result<UpgradePreviewResponse>.BadRequest("Failed to get upgrade preview from Stripe.");
        }

        var lineItems = preview.LineItems
            .Select(item => new UpgradePreviewLineItemResponse(item.Description, item.Amount, item.Currency, item.IsProration))
            .ToArray();

        return new UpgradePreviewResponse(preview.TotalAmount, preview.Currency, lineItems);
    }
}
