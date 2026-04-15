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
public sealed record UpdateTeamCommand(string Name, string? Description) : ICommand, IRequest<Result<TeamResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TeamId Id { get; init; } = null!;

    public string Name { get; } = Name.Trim();

    public string? Description { get; } = Description?.Trim();
}

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Name).Length(1, 50).WithMessage("Name must be between 1 and 50 characters.");
        RuleFor(x => x.Description).MaximumLength(255).WithMessage("Description must be at most 255 characters.");
    }
}

public sealed class UpdateTeamHandler(ITeamRepository teamRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateTeamCommand, Result<TeamResponse>>
{
    public async Task<Result<TeamResponse>> Handle(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        if (!executionContext.UserInfo.IsFeatureFlagEnabled(TeamsFeatureFlag.Key))
        {
            return Result<TeamResponse>.NotFound("Teams feature is not enabled for this tenant.");
        }

        if (executionContext.UserInfo.Role is not (nameof(UserRole.Owner) or nameof(UserRole.Admin)))
        {
            return Result<TeamResponse>.Forbidden("Only owners and admins can update teams.");
        }

        var team = await teamRepository.GetByIdAsync(command.Id, cancellationToken);
        if (team is null) return Result<TeamResponse>.NotFound($"Team with id '{command.Id}' not found.");

        var nameChanged = !string.Equals(command.Name, team.Name, StringComparison.OrdinalIgnoreCase);
        if (nameChanged && !await teamRepository.IsNameUniqueAsync(command.Name, cancellationToken))
        {
            return Result<TeamResponse>.BadRequest($"A team with the name '{command.Name}' already exists.");
        }

        team.Update(command.Name, command.Description);
        teamRepository.Update(team);

        events.CollectEvent(new TeamUpdated(team.Id));

        return TeamResponse.FromTeam(team);
    }
}
