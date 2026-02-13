using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record UpdateBillingInfoCommand(
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string? State,
    string Country,
    string Email
)
    : ICommand, IRequest<Result>;

public sealed class UpdateBillingInfoValidator : AbstractValidator<UpdateBillingInfoCommand>
{
    public UpdateBillingInfoValidator()
    {
        RuleFor(x => x.Line1).Length(1, 100).WithMessage("Address line 1 must be between 1 and 100 characters.");
        RuleFor(x => x.Line2).MaximumLength(100).WithMessage("Address line 2 must be no longer than 100 characters.");
        RuleFor(x => x.PostalCode).Length(1, 10).WithMessage("Postal code must be between 1 and 10 characters.");
        RuleFor(x => x.City).Length(1, 50).WithMessage("City must be between 1 and 50 characters.");
        RuleFor(x => x.State).MaximumLength(50).WithMessage("State must be no longer than 50 characters.");
        RuleFor(x => x.Country).Length(2).WithMessage("Country must be a 2-letter ISO country code.");
        RuleFor(x => x.Email).SetValidator(new SharedValidations.Email());
    }
}

public sealed class UpdateBillingInfoHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<UpdateBillingInfoHandler> logger
) : IRequestHandler<UpdateBillingInfoCommand, Result>
{
    public async Task<Result> Handle(UpdateBillingInfoCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage billing information.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for tenant '{TenantId}'", executionContext.TenantId);
            return Result.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No Stripe customer found. A subscription must be created first.");
        }

        var billingInfo = new BillingInfo(
            new BillingAddress(command.Line1, command.Line2, command.PostalCode, command.City, command.State, command.Country),
            command.Email
        );

        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.UpdateCustomerBillingInfoAsync(subscription.StripeCustomerId, billingInfo, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to update billing information in Stripe.");
        }

        subscription.SetBillingInfo(billingInfo);
        subscriptionRepository.Update(subscription);

        events.CollectEvent(new BillingInfoUpdated(subscription.Id));

        return Result.Success();
    }
}
