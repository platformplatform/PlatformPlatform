using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.BackOffice.Commands;

[PublicAPI]
public sealed record ChangeTicketStatusCommand(SupportTicketStatus Status) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;
}

public sealed class ChangeTicketStatusHandler(
    ISupportTicketRepository ticketRepository,
    BackOfficeStaffContext backOfficeStaffContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<ChangeTicketStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeTicketStatusCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var staff = backOfficeStaffContext.GetCurrent();
        var fromStatus = ticket.Status;
        var wasTerminal = fromStatus is SupportTicketStatus.Resolved or SupportTicketStatus.Closed;
        var outcome = ticket.ChangeStatusByStaff(command.Status, staff, timeProvider.GetUtcNow());

        switch (outcome)
        {
            case ChangeStatusByStaffOutcome.AlreadyInStatus:
                return Result.BadRequest($"Ticket is already in status '{command.Status}'.");
            case ChangeStatusByStaffOutcome.ClosingRefused:
                return Result.BadRequest("Staff cannot close a ticket directly; only the end user can close it.");
            case ChangeStatusByStaffOutcome.ReopenWindowExpired:
                return Result.BadRequest("This ticket can no longer be reopened. The user must create a new ticket.");
            case ChangeStatusByStaffOutcome.Changed:
                break;
            default:
                throw new UnreachableException($"Unhandled ChangeStatusByStaffOutcome '{outcome}'.");
        }

        ticketRepository.Update(ticket);
        if (wasTerminal)
        {
            events.CollectEvent(new SupportTicketReopened(ticket.Id, SupportMessageAuthorKind.Staff));
        }

        events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, fromStatus, ticket.Status, SupportMessageAuthorKind.Staff));
        return Result.Success();
    }
}
