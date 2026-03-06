using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Authentication;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.Email;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;
using SharedKernel.Validation;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record InviteUserCommand(string Email) : ICommand, IRequest<Result<UserResponse>>
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
) : IRequestHandler<InviteUserCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<UserResponse>.Forbidden("Only owners are allowed to invite other users.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Result<UserResponse>.Unauthorized("Tenant has been deleted.", responseHeaders: new Dictionary<string, string>
                {
                    { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
                }
            );
        }

        if (string.IsNullOrWhiteSpace(tenant.Name))
        {
            return Result<UserResponse>.BadRequest("Account name must be set before inviting users.");
        }

        if (!await userRepository.IsEmailFreeAsync(command.Email, cancellationToken))
        {
            var deletedUser = await userRepository.GetDeletedUserByEmailAsync(command.Email, cancellationToken);
            if (deletedUser is not null)
            {
                return Result<UserResponse>.BadRequest($"The user '{command.Email}' was previously deleted. Please restore or permanently delete the user before inviting again.");
            }

            return Result<UserResponse>.BadRequest($"The user '{command.Email}' already exists.");
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

        var user = await userRepository.GetByIdAsync(result.Value!, cancellationToken);
        return UserResponse.FromUser(user!);
    }
}
