using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetSubscribePreviewQuery(SubscriptionPlan Plan) : IRequest<Result<SubscribePreviewResponse>>;

[PublicAPI]
public sealed record SubscribePreviewResponse(decimal TotalAmount, string Currency, decimal TaxAmount);

public sealed class GetSubscribePreviewValidator : AbstractValidator<GetSubscribePreviewQuery>
{
    public GetSubscribePreviewValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Basis).WithMessage("Cannot preview subscription for the Basis plan.");
    }
}

public sealed class GetSubscribePreviewHandler(ISubscriptionRepository subscriptionRepository, StripeClientFactory stripeClientFactory, IExecutionContext executionContext)
    : IRequestHandler<GetSubscribePreviewQuery, Result<SubscribePreviewResponse>>
{
    public async Task<Result<SubscribePreviewResponse>> Handle(GetSubscribePreviewQuery query, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<SubscribePreviewResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            return Result<SubscribePreviewResponse>.BadRequest("Billing information must be saved before previewing subscription.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var preview = await stripeClient.GetCheckoutPreviewAsync(subscription.StripeCustomerId, query.Plan, cancellationToken);
        if (preview is null)
        {
            return Result<SubscribePreviewResponse>.BadRequest("Failed to get subscription preview from Stripe.");
        }

        return new SubscribePreviewResponse(preview.TotalAmount, preview.Currency, preview.TaxAmount);
    }
}
