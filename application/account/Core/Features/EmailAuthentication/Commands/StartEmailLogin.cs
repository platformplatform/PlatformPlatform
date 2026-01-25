using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Shared;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Commands;

[PublicAPI]
public sealed record StartEmailLoginCommand(string Email) : ICommand, IRequest<Result<StartEmailLoginResponse>>
{
    public string Email { get; init; } = Email.Trim().ToLower();
}

[PublicAPI]
public sealed record StartEmailLoginResponse(EmailLoginId EmailLoginId, int ValidForSeconds);

public sealed class StartEmailLoginValidator : AbstractValidator<StartEmailLoginCommand>
{
    public StartEmailLoginValidator()
    {
        RuleFor(x => x.Email).SetValidator(new SharedValidations.Email());
    }
}

public sealed class StartEmailLoginHandler(
    IUserRepository userRepository,
    IEmailClient emailClient,
    StartEmailConfirmation startEmailConfirmation,
    ITelemetryEventsCollector events
) : IRequestHandler<StartEmailLoginCommand, Result<StartEmailLoginResponse>>
{
    private const string UnknownUserEmailTemplate =
        """
        <h1 style="text-align:center;font-family=sans-serif;font-size:20px">You or someone else tried to login to PlatformPlatform</h1>
        <p style="text-align:center;font-family=sans-serif;font-size:16px">This request was made by entering your mail {email}, but we have no record of such user.</p>
        <p style="text-align:center;font-family=sans-serif;font-size:16px">You can sign up for an account on www.platformplatform.net/signup.</p>
        """;

    private const string LoginEmailTemplate =
        """
        <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
        <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
        <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
        """;

    public async Task<Result<StartEmailLoginResponse>> Handle(StartEmailLoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserByEmailUnfilteredAsync(command.Email, cancellationToken);

        if (user is null)
        {
            await emailClient.SendAsync(command.Email.ToLower(), "Unknown user tried to login to PlatformPlatform",
                UnknownUserEmailTemplate.Replace("{email}", command.Email),
                cancellationToken
            );

            return new StartEmailLoginResponse(EmailLoginId.NewId(), EmailLogin.ValidForSeconds);
        }

        var result = await startEmailConfirmation.StartAsync(
            user.Email, "PlatformPlatform login verification code", LoginEmailTemplate, EmailLoginType.Login, cancellationToken
        );

        if (!result.IsSuccess) return Result<StartEmailLoginResponse>.From(result);

        events.CollectEvent(new EmailLoginStarted(user.Id));

        return new StartEmailLoginResponse(result.Value!, EmailLogin.ValidForSeconds);
    }
}
