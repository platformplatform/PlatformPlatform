using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Teams.Commands;

[PublicAPI]
public sealed record CreateTeamCommand(string Name, string? Description)
    : ICommand, IRequest<Result<TeamId>>
{
    public string Name { get; init; } = Name.Trim();
}

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name).Length(1, 100).WithMessage("Name must be between 1 and 100 characters.");
        When(x => x.Description is not null, () => { RuleFor(x => x.Description!).MaximumLength(500).WithMessage("Description must be at most 500 characters."); });
    }
}

public sealed class CreateTeamHandler(
    ITeamRepository teamRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<CreateTeamCommand, Result<TeamId>>
{
    public async Task<Result<TeamId>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<TeamId>.Forbidden("Only tenant owners can create teams.");
        }

        if (await teamRepository.GetByNameAsync(command.Name, cancellationToken) is not null)
        {
            return Result<TeamId>.Conflict($"A team with the name '{command.Name}' already exists.");
        }

        var team = Team.Create(executionContext.TenantId!, command.Name, command.Description);
        await teamRepository.AddAsync(team, cancellationToken);

        events.CollectEvent(new TeamCreated(team.Id));

        return team.Id;
    }
}
