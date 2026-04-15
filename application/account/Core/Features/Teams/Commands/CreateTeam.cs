using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Teams.Commands;

[PublicAPI]
public sealed record CreateTeamCommand(string Name, string? Description) : ICommand, IRequest<Result<TeamResponse>>
{
    public string Name { get; } = Name.Trim();

    public string? Description { get; } = Description?.Trim();
}

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.Name).Length(1, 50).WithMessage("Name must be between 1 and 50 characters.");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("Description must be at most 255 characters.");
    }
}

public sealed class CreateTeamHandler(ITeamRepository teamRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<CreateTeamCommand, Result<TeamResponse>>
{
    public async Task<Result<TeamResponse>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        if (!executionContext.UserInfo.IsFeatureFlagEnabled(TeamsFeatureFlag.Key))
        {
            return Result<TeamResponse>.NotFound("Teams feature is not enabled for this tenant.");
        }

        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result<TeamResponse>.Forbidden("Only owners and admins can create teams.");
        }

        if (!await teamRepository.IsNameUniqueAsync(command.Name, cancellationToken))
        {
            return Result<TeamResponse>.BadRequest($"A team with the name '{command.Name}' already exists.");
        }

        var team = Team.Create(executionContext.TenantId!, command.Name, command.Description);
        await teamRepository.AddAsync(team, cancellationToken);

        events.CollectEvent(new TeamCreated(team.Id));

        return TeamResponse.FromTeam(team);
    }
}
