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
    string Name,
    string Line1,
    string? Line2,
    string PostalCode,
    string City,
    string? State,
    string Country,
    string Email,
    string? TaxId
)
    : ICommand, IRequest<Result>;

public sealed class UpdateBillingInfoValidator : AbstractValidator<UpdateBillingInfoCommand>
{
    public UpdateBillingInfoValidator()
    {
        RuleFor(x => x.Name).Length(1, 100).WithMessage("Name must be between 1 and 100 characters.");
        RuleFor(x => x.Line1).Length(1, 100).WithMessage("Address line 1 must be between 1 and 100 characters.");
        RuleFor(x => x.Line2).MaximumLength(100).WithMessage("Address line 2 must be no longer than 100 characters.");
        RuleFor(x => x.PostalCode).Length(1, 10).WithMessage("Postal code must be between 1 and 10 characters.");
        RuleFor(x => x.City).Length(1, 50).WithMessage("City must be between 1 and 50 characters.");
        RuleFor(x => x.State).MaximumLength(50).WithMessage("State must be no longer than 50 characters.");
        RuleFor(x => x.Country).Length(2).WithMessage("Country must be a 2-letter ISO country code.");
        RuleFor(x => x.Email).SetValidator(new SharedValidations.Email());
        RuleFor(x => x.TaxId).MaximumLength(20).WithMessage("Tax ID must be no longer than 20 characters.");
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

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        var stripeClient = stripeClientFactory.GetClient();

        if (subscription.StripeCustomerId is null)
        {
            if (executionContext.UserInfo.Email is null)
            {
                return Result.BadRequest("User email is required to create a Stripe customer.");
            }

            var customerId = await stripeClient.CreateCustomerAsync(command.Name, executionContext.UserInfo.Email, subscription.TenantId.Value, cancellationToken);
            if (customerId is null)
            {
                return Result.BadRequest("Failed to create Stripe customer.");
            }

            subscription.SetStripeCustomerId(customerId);
            subscriptionRepository.Update(subscription);
        }

        var billingInfo = new BillingInfo(
            command.Name,
            new BillingAddress(command.Line1, command.Line2, command.PostalCode, command.City, command.State, command.Country),
            command.Email,
            command.TaxId
        );

        var success = await stripeClient.UpdateCustomerBillingInfoAsync(subscription.StripeCustomerId!, billingInfo, executionContext.UserInfo.Locale, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to update billing information in Stripe.");
        }

        if (command.TaxId != subscription.BillingInfo?.TaxId)
        {
            await stripeClient.SyncCustomerTaxIdAsync(subscription.StripeCustomerId!, command.TaxId, cancellationToken);
        }

        subscription.SetBillingInfo(billingInfo);
        subscriptionRepository.Update(subscription);

        events.CollectEvent(new BillingInfoUpdated(subscription.Id));

        return Result.Success();
    }
}
