using Account.Features.SupportTickets.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.Commands;

[PublicAPI]
public sealed record SubmitCsatCommand(SupportTicketCsatScore Score, string? Comment) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;
}

public sealed class SubmitCsatValidator : AbstractValidator<SubmitCsatCommand>
{
    public SubmitCsatValidator()
    {
        RuleFor(x => x.Comment!).MaximumLength(SupportTicket.CsatCommentMaxLength)
            .WithMessage($"CSAT comment must be at most {SupportTicket.CsatCommentMaxLength} characters.")
            .When(x => x.Comment is not null);
    }
}

public sealed class SubmitCsatHandler(
    ISupportTicketRepository ticketRepository,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<SubmitCsatCommand, Result>
{
    public async Task<Result> Handle(SubmitCsatCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        if (ticket.ReporterId != executionContext.UserInfo.Id!) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        // CSAT can only be submitted once a ticket has reached a terminal state (Resolved going
        // forward, or the legacy Closed for historical rows). The domain method itself does not
        // change status; this guard keeps the API contract aligned with the user-facing CSAT flow.
        if (ticket.Status is not (SupportTicketStatus.Resolved or SupportTicketStatus.Closed))
        {
            return Result.BadRequest("A rating can only be submitted on a resolved or closed ticket.");
        }

        // An existing rating may only be overwritten when it's stale (i.e. the ticket was reopened
        // since the rating was submitted). Otherwise the rating is final.
        if (ticket.Csat is not null && !ticket.IsCsatStale())
        {
            return Result.BadRequest("This ticket already has a rating.");
        }

        ticket.SubmitCsat(command.Score, command.Comment, timeProvider.GetUtcNow());
        ticketRepository.Update(ticket);

        events.CollectEvent(new SupportTicketCsatSubmitted(ticket.Id, command.Score));
        return Result.Success();
    }
}
