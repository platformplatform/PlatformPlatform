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

        if (command.Status is SupportTicketStatus.Closed)
        {
            return Result.BadRequest("Staff cannot close a ticket directly — only the end user can close it.");
        }

        var staff = backOfficeStaffContext.GetCurrent();
        var fromStatus = ticket.Status;
        if (!ticket.ChangeStatusByStaff(command.Status, staff, timeProvider.GetUtcNow()))
        {
            return Result.BadRequest($"Ticket is already in status '{command.Status}'.");
        }

        ticketRepository.Update(ticket);
        events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, fromStatus, ticket.Status, SupportMessageAuthorKind.Staff));
        return Result.Success();
    }
}
