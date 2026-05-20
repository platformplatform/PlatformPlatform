using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.BackOffice.Commands;

[PublicAPI]
public sealed record AssignTicketCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;

    // Setting both to null assigns to no one (clears the assignee).
    public string? AssigneeObjectId { get; init; }

    public string? AssigneeDisplayName { get; init; }
}

public sealed class AssignTicketHandler(
    ISupportTicketRepository ticketRepository,
    BackOfficeStaffContext backOfficeStaffContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<AssignTicketCommand, Result>
{
    public async Task<Result> Handle(AssignTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var actor = backOfficeStaffContext.GetCurrent();
        BackOfficeStaffRef? assignee = null;
        if (!string.IsNullOrWhiteSpace(command.AssigneeObjectId))
        {
            assignee = new BackOfficeStaffRef(command.AssigneeObjectId, command.AssigneeDisplayName ?? command.AssigneeObjectId);
        }

        if (!ticket.Assign(assignee, actor, timeProvider.GetUtcNow()))
        {
            return Result.BadRequest("Ticket is already assigned to that staff user.");
        }

        ticketRepository.Update(ticket);
        events.CollectEvent(new SupportTicketAssigneeChanged(ticket.Id, assignee?.ObjectId));
        return Result.Success();
    }
}
