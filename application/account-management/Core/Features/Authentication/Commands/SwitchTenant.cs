using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record SwitchTenantCommand(TenantId TenantId) : ICommand, IRequest<Result>;

public sealed class SwitchTenantHandler(
    IUserRepository userRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<SwitchTenantHandler> logger
) : IRequestHandler<SwitchTenantCommand, Result>
{
    public async Task<Result> Handle(SwitchTenantCommand command, CancellationToken cancellationToken)
    {
        var currentUserEmail = executionContext.UserInfo.Email;
        if (currentUserEmail is null)
        {
            return Result.BadRequest("User email not found in claims.");
        }

        var users = await userRepository.GetUsersByEmailUnfilteredAsync(currentUserEmail, cancellationToken);
        var targetUser = users.FirstOrDefault(u => u.TenantId == command.TenantId);
        if (targetUser is null)
        {
            logger.LogWarning("UserId '{UserId}' does not have access to TenantID '{TennantId}'", executionContext.UserInfo.Id, command.TenantId);
            return Result.Forbidden($"User does not have access to tenant '{command.TenantId}'.");
        }

        var userInfo = await userInfoFactory.CreateUserInfoAsync(targetUser, cancellationToken);
        authenticationTokenService.CreateAndSetAuthenticationTokens(userInfo);

        events.CollectEvent(new TenantSwitched(executionContext.TenantId!, command.TenantId, targetUser.Id));

        return Result.Success();
    }
}
