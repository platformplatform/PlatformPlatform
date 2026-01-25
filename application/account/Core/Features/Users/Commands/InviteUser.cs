using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.Account.Features.Users.Commands;

[PublicAPI]
public sealed record InviteUserCommand(string Email) : ICommand, IRequest<Result>
{
    public string Email { get; init; } = Email.Trim().ToLower();
}

public sealed class InviteUserValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserValidator()
    {
        RuleFor(x => x.Email).SetValidator(new SharedValidations.Email());
    }
}

public sealed class InviteUserHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IEmailClient emailClient,
    IExecutionContext executionContext,
    IMediator mediator,
    ITelemetryEventsCollector events
) : IRequestHandler<InviteUserCommand, Result>
{
    public async Task<Result> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to invite other users.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Result.Unauthorized("Tenant has been deleted.", responseHeaders: new Dictionary<string, string>
                {
                    { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
                }
            );
        }

        if (string.IsNullOrWhiteSpace(tenant.Name))
        {
            return Result.BadRequest("Account name must be set before inviting users.");
        }

        if (!await userRepository.IsEmailFreeAsync(command.Email, cancellationToken))
        {
            var deletedUser = await userRepository.GetDeletedUserByEmailAsync(command.Email, cancellationToken);
            if (deletedUser is not null)
            {
                return Result.BadRequest($"The user '{command.Email}' was previously deleted. Please restore or permanently delete the user before inviting again.");
            }

            return Result.BadRequest($"The user '{command.Email}' already exists.");
        }

        var result = await mediator.Send(
            new CreateUserCommand(executionContext.TenantId!, command.Email, UserRole.Member, false, null), cancellationToken
        );

        events.CollectEvent(new UserInvited(result.Value!));

        var loginPath = $"{Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)}/login";
        var inviter = $"{executionContext.UserInfo.FirstName} {executionContext.UserInfo.LastName}".Trim();
        inviter = inviter.Length > 0 ? inviter : executionContext.UserInfo.Email;
        await emailClient.SendAsync(command.Email.ToLower(), $"You have been invited to join {tenant.Name} on PlatformPlatform",
            $"""
             <h1 style="text-align:center;font-family:sans-serif;font-size:20px">
               <b>{inviter}</b> invited you to join PlatformPlatform.
             </h1>
             <p style="text-align:center;font-family:sans-serif;font-size:16px">
               To gain access, <a href="{loginPath}" target="blank">go to this page in your open browser</a> and login using <b>{command.Email.ToLower()}</b>.
             </p>
             """,
            cancellationToken
        );

        return Result.Success();
    }
}
