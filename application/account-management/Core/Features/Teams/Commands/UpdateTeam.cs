using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Teams.Commands;

[PublicAPI]
public sealed record UpdateTeamCommand(string Name, string? Description) : ICommand, IRequest<Result>
{
    public string Name { get; init; } = Name.Trim();

    [JsonIgnore] // Removes from API contract
    public TeamId Id { get; init; } = null!;
}

public sealed class UpdateTeamValidator : AbstractValidator<UpdateTeamCommand>
{
    public UpdateTeamValidator()
    {
        RuleFor(x => x.Name).Length(1, 100).WithMessage("Name must be between 1 and 100 characters.");
        When(x => x.Description is not null, () => { RuleFor(x => x.Description!).MaximumLength(500).WithMessage("Description must be at most 500 characters."); });
    }
}

public sealed class UpdateTeamHandler(
    ITeamRepository teamRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<UpdateTeamCommand, Result>
{
    public async Task<Result> Handle(UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only tenant owners can update teams.");
        }

        var team = await teamRepository.GetByIdAsync(command.Id, cancellationToken);

        if (team is null)
        {
            return Result.NotFound($"Team with ID '{command.Id}' not found.");
        }

        if (team.Name != command.Name)
        {
            var existingTeam = await teamRepository.GetByNameAsync(command.Name, cancellationToken);
            if (existingTeam is not null && existingTeam.Id != command.Id)
            {
                return Result.Conflict($"A team with the name '{command.Name}' already exists.");
            }
        }

        team.Update(command.Name, command.Description);
        teamRepository.Update(team);

        events.CollectEvent(new TeamUpdated(team.Id));

        return Result.Success();
    }
}
