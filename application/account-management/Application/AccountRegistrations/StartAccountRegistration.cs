using FluentValidation;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.AccountRegistrations;

[UsedImplicitly]
public sealed record StartAccountRegistrationCommand(string Email, string FirstName, string LastName)
    : ICommand, IRequest<Result<AccountRegistrationId>>;

[UsedImplicitly]
public sealed class StartAccountRegistrationCommandHandler(
    IAccountRegistrationRepository accountRegistrationRepository,
    IEmailService emailService,
    ITelemetryEventsCollector events
) : IRequestHandler<StartAccountRegistrationCommand, Result<AccountRegistrationId>>
{
    public async Task<Result<AccountRegistrationId>> Handle(
        StartAccountRegistrationCommand command,
        CancellationToken cancellationToken
    )
    {
        var existingAccountRegistrations = accountRegistrationRepository.GetByEmail(command.Email);

        if (existingAccountRegistrations.Any(r => !r.HasExpired()))
        {
            return Result<AccountRegistrationId>.Conflict(
                "Account registration for this mail has already been started. Please check your spam folder.");
        }

        if (existingAccountRegistrations.Count(r => r.CompletedAt > TimeProvider.System.GetUtcNow().AddDays(-1)) > 3)
        {
            return Result<AccountRegistrationId>.TooManyRequests(
                "Too many attempts to register this email address. Please try again later.");
        }

        var accountRegistration = AccountRegistration.Create(command.Email, command.FirstName, command.LastName);
        await accountRegistrationRepository.AddAsync(accountRegistration, cancellationToken);
        events.CollectEvent(new AccountRegistrationStarted());

        await emailService.SendAsync(accountRegistration.Email, "Confirm your email address",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{accountRegistration.OneTimePassword}</p>
             """, cancellationToken);

        return accountRegistration.Id;
    }
}

[UsedImplicitly]
public sealed class StartAccountRegistrationValidator : AbstractValidator<StartAccountRegistrationCommand>
{
    public StartAccountRegistrationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name must be between 1 and 30 characters.")
            .Length(1, 30).WithMessage("First name must be between 1 and 30 characters.");
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("First name must be between 1 and 30 characters.")
            .Length(1, 30).WithMessage("First name must be between 1 and 30 characters.");
    }
}