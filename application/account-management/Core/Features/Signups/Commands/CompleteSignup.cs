using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Signups.Commands;

[PublicAPI]
public sealed record CompleteSignupCommand(string OneTimePassword, string PreferredLocale) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailConfirmationId EmailConfirmationId { get; init; } = null!;
}

public sealed class CompleteSignupHandler(
    IUserRepository userRepository,
    AuthenticationTokenService authenticationTokenService,
    IMediator mediator,
    ITelemetryEventsCollector events
) : IRequestHandler<CompleteSignupCommand, Result>
{
    public async Task<Result> Handle(CompleteSignupCommand command, CancellationToken cancellationToken)
    {
        var completeEmailConfirmationResult = await mediator.Send(
            new CompleteEmailConfirmationCommand(command.EmailConfirmationId, command.OneTimePassword),
            cancellationToken
        );

        if (!completeEmailConfirmationResult.IsSuccess) return Result.From(completeEmailConfirmationResult);

        var createTenantResult = await mediator.Send(
            new CreateTenantCommand(completeEmailConfirmationResult.Value!.Email, true, command.PreferredLocale),
            cancellationToken
        );

        if (!createTenantResult.IsSuccess) return Result.From(createTenantResult);

        var user = await userRepository.GetByIdAsync(createTenantResult.Value!.UserId, cancellationToken);
        authenticationTokenService.CreateAndSetAuthenticationTokens(user!.Adapt<UserInfo>());

        events.CollectEvent(
            new SignupCompleted(createTenantResult.Value.TenantId, completeEmailConfirmationResult.Value!.ConfirmationTimeInSeconds)
        );
        return Result.Success();
    }
}
