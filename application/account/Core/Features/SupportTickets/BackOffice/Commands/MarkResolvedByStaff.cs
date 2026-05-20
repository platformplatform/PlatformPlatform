using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.BackOffice.Commands;

[PublicAPI]
public sealed record MarkResolvedByStaffCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;
}

public sealed class MarkResolvedByStaffHandler(
    ISupportTicketRepository ticketRepository,
    BackOfficeStaffContext backOfficeStaffContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<MarkResolvedByStaffCommand, Result>
{
    public async Task<Result> Handle(MarkResolvedByStaffCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var staff = backOfficeStaffContext.GetCurrent();
        var fromStatus = ticket.Status;
        if (!ticket.MarkResolvedByStaff(staff, timeProvider.GetUtcNow()))
        {
            return Result.BadRequest("Ticket is already resolved or closed.");
        }

        ticketRepository.Update(ticket);
        events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, fromStatus, ticket.Status, SupportMessageAuthorKind.Staff));
        return Result.Success();
    }
}
