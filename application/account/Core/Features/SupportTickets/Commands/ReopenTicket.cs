using Account.Features.SupportTickets.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.Commands;

[PublicAPI]
public sealed record ReopenTicketCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;
}

public sealed class ReopenTicketHandler(
    ISupportTicketRepository ticketRepository,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<ReopenTicketCommand, Result>
{
    public async Task<Result> Handle(ReopenTicketCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        if (ticket.ReporterId != executionContext.UserInfo.Id!) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        if (!ticket.ReopenByUser(timeProvider.GetUtcNow()))
        {
            return Result.BadRequest("This ticket can no longer be reopened. Please create a new ticket instead.");
        }

        ticketRepository.Update(ticket);
        events.CollectEvent(new SupportTicketReopened(ticket.Id, SupportMessageAuthorKind.User));
        return Result.Success();
    }
}
