using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Shared;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Commands;

[PublicAPI]
public sealed record StartEmailSignupCommand(string Email) : ICommand, IRequest<Result<StartEmailSignupResponse>>
{
    public string Email { get; } = Email.Trim().ToLower();
}

[PublicAPI]
public sealed record StartEmailSignupResponse(EmailLoginId EmailLoginId, int ValidForSeconds);

public sealed class StartEmailSignupValidator : AbstractValidator<StartEmailSignupCommand>
{
    public StartEmailSignupValidator()
    {
        RuleFor(x => x.Email).SetValidator(new SharedValidations.Email());
    }
}

public sealed class StartEmailSignupHandler(StartEmailConfirmation startEmailConfirmation, ITelemetryEventsCollector events)
    : IRequestHandler<StartEmailSignupCommand, Result<StartEmailSignupResponse>>
{
    public async Task<Result<StartEmailSignupResponse>> Handle(StartEmailSignupCommand command, CancellationToken cancellationToken)
    {
        var result = await startEmailConfirmation.StartAsync(
            command.Email,
            "Confirm your email address",
            """
            <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
            <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
            <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
            """,
            EmailLoginType.Signup,
            cancellationToken
        );

        if (!result.IsSuccess) return Result<StartEmailSignupResponse>.From(result);

        events.CollectEvent(new SignupStarted());

        return Result<StartEmailSignupResponse>.Success(new StartEmailSignupResponse(result.Value!, EmailLogin.ValidForSeconds));
    }
}
