using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetCheckoutPreviewQuery(SubscriptionPlan Plan) : IRequest<Result<CheckoutPreviewResponse>>;

[PublicAPI]
public sealed record CheckoutPreviewResponse(decimal TotalAmount, string Currency, decimal TaxAmount);

public sealed class GetCheckoutPreviewValidator : AbstractValidator<GetCheckoutPreviewQuery>
{
    public GetCheckoutPreviewValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Basis).WithMessage("Cannot preview checkout for the Basis plan.");
    }
}

public sealed class GetCheckoutPreviewHandler(ISubscriptionRepository subscriptionRepository, StripeClientFactory stripeClientFactory, IExecutionContext executionContext)
    : IRequestHandler<GetCheckoutPreviewQuery, Result<CheckoutPreviewResponse>>
{
    public async Task<Result<CheckoutPreviewResponse>> Handle(GetCheckoutPreviewQuery query, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<CheckoutPreviewResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            return Result<CheckoutPreviewResponse>.BadRequest("Billing information must be saved before previewing checkout.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var preview = await stripeClient.GetCheckoutPreviewAsync(subscription.StripeCustomerId, query.Plan, cancellationToken);
        if (preview is null)
        {
            return Result<CheckoutPreviewResponse>.BadRequest("Failed to get checkout preview from Stripe.");
        }

        return new CheckoutPreviewResponse(preview.TotalAmount, preview.Currency, preview.TaxAmount);
    }
}
