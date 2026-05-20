using Account.Features.SupportTickets.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.Commands;

[PublicAPI]
public sealed record CloseTicketByUserCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;

    public SupportTicketCsatScore? CsatScore { get; init; }

    public string? CsatComment { get; init; }
}

public sealed class CloseTicketByUserValidator : AbstractValidator<CloseTicketByUserCommand>
{
    public CloseTicketByUserValidator()
    {
        RuleFor(x => x.CsatComment!).MaximumLength(SupportTicket.CsatCommentMaxLength)
            .WithMessage($"CSAT comment must be at most {SupportTicket.CsatCommentMaxLength} characters.")
            .When(x => x.CsatComment is not null);
    }
}

public sealed class CloseTicketByUserHandler(
    ISupportTicketRepository ticketRepository,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<CloseTicketByUserCommand, Result>
{
    public async Task<Result> Handle(CloseTicketByUserCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        if (ticket.ReporterId != executionContext.UserInfo.Id!) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var now = timeProvider.GetUtcNow();
        // The end-user "Close" action drives the ticket to Resolved; the optional CSAT score is
        // recorded alongside. SubmitCsat no longer changes status, so the transition is explicit.
        // CloseByUser is a no-op on an already-terminal ticket, which is acceptable when the user
        // is submitting a CSAT rating after the fact; without a CSAT score there is nothing to do.
        var transitioned = ticket.CloseByUser(now);
        if (!transitioned && command.CsatScore is null)
        {
            return Result.BadRequest("Ticket is already closed.");
        }

        if (command.CsatScore is not null)
        {
            // When a rating already exists, only allow overwriting it after a reopen made it stale.
            // Without this guard, a user could replay /close with a different score and overwrite a
            // final rating; the standalone /csat endpoint enforces the same rule.
            if (ticket.Csat is not null && !ticket.IsCsatStale())
            {
                return Result.BadRequest("This ticket already has a rating.");
            }

            ticket.SubmitCsat(command.CsatScore.Value, command.CsatComment, now);
            events.CollectEvent(new SupportTicketCsatSubmitted(ticket.Id, command.CsatScore.Value));
        }

        ticketRepository.Update(ticket);
        // Only emit the close event when the ticket actually transitioned. The after-the-fact CSAT
        // path (already terminal, CSAT supplied) does not represent a new close and would otherwise
        // double-count in telemetry alongside the original transition.
        if (transitioned) events.CollectEvent(new SupportTicketClosed(ticket.Id, SupportMessageAuthorKind.User));
        return Result.Success();
    }
}
