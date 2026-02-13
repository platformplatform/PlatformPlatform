using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record UpdateBillingInfoCommand(
    string? Line1,
    string? Line2,
    string? PostalCode,
    string? City,
    string? State,
    string? Country,
    string? Email
)
    : ICommand, IRequest<Result>;

public sealed class UpdateBillingInfoValidator : AbstractValidator<UpdateBillingInfoCommand>
{
    public UpdateBillingInfoValidator()
    {
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null).WithMessage("Email must be a valid email address.");
    }
}

public sealed class UpdateBillingInfoHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
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
            return Result.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.StripeCustomerId is null)
        {
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
