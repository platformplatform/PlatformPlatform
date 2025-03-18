using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record StartLoginCommand(string Email) : ICommand, IRequest<Result<StartLoginResponse>>
{
    public string Email { get; init; } = Email.Trim().ToLower();
}

[PublicAPI]
public sealed record StartLoginResponse(LoginId LoginId, EmailConfirmationId EmailConfirmationId, int ValidForSeconds);

public sealed class StartLoginValidator : AbstractValidator<StartLoginCommand>
{
    public StartLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
    }
}

public sealed class StartLoginHandler(
    IUserRepository userRepository,
    ILoginRepository loginRepository,
    IEmailClient emailClient,
    IMediator mediator,
    ITelemetryEventsCollector events
) : IRequestHandler<StartLoginCommand, Result<StartLoginResponse>>
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

    public async Task<Result<StartLoginResponse>> Handle(StartLoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserByEmailUnfilteredAsync(command.Email, cancellationToken);

        if (user is null)
        {
            await emailClient.SendAsync(command.Email.ToLower(), "Unknown user tried to login to PlatformPlatform",
                UnknownUserEmailTemplate.Replace("{email}", command.Email),
                cancellationToken
            );

            // Return a fake login process id to the client, so an attacker can't guess if the email is valid or not
            return new StartLoginResponse(LoginId.NewId(), EmailConfirmationId.NewId(), EmailConfirmation.ValidForSeconds);
        }

        var result = await mediator.Send(
            new StartEmailConfirmationCommand(
                user.Email,
                "PlatformPlatform login verification code",
                LoginEmailTemplate,
                EmailConfirmationType.Login
            ),
            cancellationToken
        );

        if (!result.IsSuccess) return Result<StartLoginResponse>.From(result);

        var login = Login.Create(user, result.Value!.EmailConfirmationId);
        await loginRepository.AddAsync(login, cancellationToken);
        events.CollectEvent(new LoginStarted(user.Id));

        return new StartLoginResponse(login.Id, login.EmailConfirmationId, EmailConfirmation.ValidForSeconds);
    }
}
