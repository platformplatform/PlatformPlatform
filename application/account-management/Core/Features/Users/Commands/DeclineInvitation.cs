using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record DeclineInvitationCommand(TenantId TenantId) : ICommand, IRequest<Result>;

public sealed class DeclineInvitationHandler(
    IUserRepository userRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<DeclineInvitationCommand, Result>
{
    public async Task<Result> Handle(DeclineInvitationCommand command, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetUsersByEmailUnfilteredAsync(executionContext.UserInfo.Email!, cancellationToken);

        // Find user for the specific tenant
        var user = users.SingleOrDefault(u => u.TenantId == command.TenantId);

        if (user is null)
        {
            return Result.NotFound("The invitation has been revoked.");
        }

        if (user.EmailConfirmed)
        {
            return Result.BadRequest("This invitation has already been accepted.");
        }

        // Calculate how long the invitation existed
        var inviteExistedTimeInMinutes = (int)(TimeProvider.System.GetUtcNow() - user.CreatedAt).TotalMinutes;

        // Delete the user to decline the invitation
        userRepository.Remove(user);

        events.CollectEvent(new UserInviteDeclined(user.Id, inviteExistedTimeInMinutes));

        return Result.Success();
    }
}
