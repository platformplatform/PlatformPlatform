using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CheckoutSuccessCommand(string SessionId) : ICommand, IRequest<Result>;

public sealed class CheckoutSuccessValidator : AbstractValidator<CheckoutSuccessCommand>
{
    public CheckoutSuccessValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Session ID is required.");
    }
}

public sealed class CheckoutSuccessHandler : IRequestHandler<CheckoutSuccessCommand, Result>
{
    public Task<Result> Handle(CheckoutSuccessCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}
