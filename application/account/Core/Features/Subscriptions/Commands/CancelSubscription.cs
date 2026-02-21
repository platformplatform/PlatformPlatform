using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CancelSubscriptionCommand(CancellationReason Reason, string? Feedback) : ICommand, IRequest<Result>
{
    public string? Feedback { get; } = Feedback?.Trim();
}

public sealed class CancelSubscriptionValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionValidator()
    {
        RuleFor(x => x.Feedback)
            .MaximumLength(500)
            .WithMessage("Feedback must be no longer than 500 characters.")
            .Must(feedback => !feedback!.Contains('<') && !feedback.Contains('>'))
            .WithMessage("Feedback must be no longer than 500 characters.")
            .When(x => x.Feedback is not null);
    }
}

public sealed class CancelSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ILogger<CancelSubscriptionHandler> logger
) : IRequestHandler<CancelSubscriptionCommand, Result>
{
    public async Task<Result> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.Plan == SubscriptionPlan.Basis)
        {
            return Result.BadRequest("Cannot cancel a Basis subscription.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No active Stripe subscription found.");
        }

        if (subscription.CancelAtPeriodEnd)
        {
            return Result.BadRequest("Subscription is already scheduled for cancellation.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.CancelSubscriptionAtPeriodEndAsync(subscription.StripeSubscriptionId, command.Reason, command.Feedback, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to cancel subscription in Stripe.");
        }

        // Subscription is updated and telemetry is collected in ProcessPendingStripeEvents when Stripe confirms the state change via webhook

        return Result.Success();
    }
}
