using FluentValidation;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.AccountRegistrations;

[UsedImplicitly]
public sealed record StartAccountRegistrationCommand(string Email)
    : ICommand, IRequest<Result<AccountRegistrationId>>;

[UsedImplicitly]
public sealed class StartAccountRegistrationCommandHandler(
    IAccountRegistrationRepository accountRegistrationRepository,
    ISmtpEmailSender emailSender,
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
            return Result<AccountRegistrationId>.BadRequest(
                "Account registration for this mail has already been started. Please check your spam folder.");
        }

        if (existingAccountRegistrations.Count(r => r.CompletedAt > TimeProvider.System.GetUtcNow().AddDays(-1)) > 3)
        {
            return Result<AccountRegistrationId>.BadRequest(
                "Too many attempts to register this email address. Please try again later.");
        }

        var accountRegistration = AccountRegistration.Create(command.Email);
        await accountRegistrationRepository.AddAsync(accountRegistration, cancellationToken);
        events.CollectEvent(new AccountRegistrationStarted());

        await emailSender.SendEmailAsync(accountRegistration.Email, "Confirm your email address",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{accountRegistration.OneTimePassword}</p>
             """);

        return accountRegistration.Id;
    }
}

[UsedImplicitly]
public sealed class StartAccountRegistrationValidator: AbstractValidator<StartAccountRegistrationCommand>
{
    public StartAccountRegistrationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
    }
}