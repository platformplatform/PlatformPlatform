using Account.Features.SupportTickets.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.Commands;

[PublicAPI]
public sealed record MarkResolvedByUserCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;
}

public sealed class MarkResolvedByUserHandler(
    ISupportTicketRepository ticketRepository,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<MarkResolvedByUserCommand, Result>
{
    public async Task<Result> Handle(MarkResolvedByUserCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        if (ticket.ReporterId != executionContext.UserInfo.Id!) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var fromStatus = ticket.Status;
        if (!ticket.MarkResolvedByUser(timeProvider.GetUtcNow()))
        {
            return Result.BadRequest("Ticket is already resolved or closed.");
        }

        ticketRepository.Update(ticket);
        events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, fromStatus, ticket.Status, SupportMessageAuthorKind.User));
        return Result.Success();
    }
}
