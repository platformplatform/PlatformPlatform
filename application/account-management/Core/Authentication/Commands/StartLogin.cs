using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Authentication.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Services;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Authentication.Commands;

[PublicAPI]
public sealed record StartLoginCommand(string Email) : ICommand, IRequest<Result<StartLoginResponse>>;

[PublicAPI]
public sealed record StartLoginResponse(string LoginId, int ValidForSeconds);

public sealed class StartLoginValidator : AbstractValidator<StartLoginCommand>
{
    public StartLoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
    }
}

public sealed class StartLoginCommandHandler(
    IUserRepository userRepository,
    ILoginRepository loginRepository,
    IEmailService emailService,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events
) : IRequestHandler<StartLoginCommand, Result<StartLoginResponse>>
{
    public async Task<Result<StartLoginResponse>> Handle(StartLoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetUserByEmailAsync(command.Email, cancellationToken);

        if (user is null)
        {
            await emailService.SendAsync(command.Email.ToLower(), "Unknown user tried to login to PlatformPlatform",
                $"""
                 <h1 style="text-align:center;font-family=sans-serif;font-size:20px">You or someone else tried to login to PlatformPlatform</h1>
                 <p style="text-align:center;font-family=sans-serif;font-size:16px">This request was made by entering your mail {command.Email}, but we have not record of such user.</p>
                 <p style="text-align:center;font-family=sans-serif;font-size:16px">You can sign up for an account on www.platformplatform.net/signup.</p>
                 """,
                cancellationToken
            );

            // Return a fake login process id to the client, so an attacker can't guess if the email is valid or not.
            // Please note that a sophisticated attacker can still guess if the email is valid or not by measuring the time it takes to get a response
            return new StartLoginResponse(LoginId.NewId(), Login.ValidForSeconds);
        }

        // TODO: Check if a login process is already started for this user and if it is not expired yet

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);

        var login = Login.Create(user, oneTimePasswordHash);

        await loginRepository.AddAsync(login, cancellationToken);
        events.CollectEvent(new LoginStarted(user.Id));

        await emailService.SendAsync(user.Email, "PlatformPlatform login verification code",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
             """,
            cancellationToken
        );

        return new StartLoginResponse(login.Id, Login.ValidForSeconds);
    }
}
