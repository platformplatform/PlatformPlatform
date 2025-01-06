using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record ResendLoginCodeCommand : ICommand, IRequest<Result<ResendLoginCodeResponse>>
{
    [JsonIgnore] // Removes this property from the API contract
    public LoginId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record ResendLoginCodeResponse(int ValidForSeconds);

public sealed class ResendLoginCodeCommandHandler(
    ILoginRepository loginRepository,
    IUserRepository userRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    ILogger<ResendLoginCodeCommandHandler> logger
) : IRequestHandler<ResendLoginCodeCommand, Result<ResendLoginCodeResponse>>
{
    public async Task<Result<ResendLoginCodeResponse>> Handle(ResendLoginCodeCommand command, CancellationToken cancellationToken)
    {
        var login = await loginRepository.GetByIdAsync(command.Id, cancellationToken);
        if (login is null)
        {
            return Result<ResendLoginCodeResponse>.NotFound($"Login with id '{command.Id}' not found.");
        }

        if (login.Completed)
        {
            logger.LogWarning("Login with id '{LoginId}' has already been completed.", login.Id);
            return Result<ResendLoginCodeResponse>.BadRequest($"The login with id {login.Id} has already been completed.");
        }

        if (login.ModifiedAt > TimeProvider.System.GetUtcNow().AddSeconds(-30))
        {
            return Result<ResendLoginCodeResponse>.BadRequest("You must wait at least 30 seconds before requesting a new code.");
        }

        var user = await userRepository.GetByIdUnfilteredAsync(login.UserId, cancellationToken);

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        login.UpdateVerificationCode(oneTimePasswordHash);
        loginRepository.Update(login);

        var secondsSinceLoginStarted = (TimeProvider.System.GetUtcNow() - login.CreatedAt).TotalSeconds;
        events.CollectEvent(new LoginCodeResend(login.UserId, (int)secondsSinceLoginStarted));

        await emailClient.SendAsync(user!.Email, "PlatformPlatform login verification code",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
             """,
            cancellationToken
        );

        return new ResendLoginCodeResponse(Login.ValidForSeconds);
    }
}
