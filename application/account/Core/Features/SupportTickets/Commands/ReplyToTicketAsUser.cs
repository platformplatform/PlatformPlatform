using System.Collections.Immutable;
using Account.Features.SupportTickets.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.Commands;

[PublicAPI]
public sealed record ReplyToTicketAsUserCommand(string Body) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;

    public string Body { get; init; } = Body.Trim();

    [JsonIgnore] // Multipart bound separately
    public ImmutableArray<SupportMessageAttachment> Attachments { get; init; } = [];

    public bool MarkAsResolved { get; init; }
}

public sealed class ReplyToTicketAsUserValidator : AbstractValidator<ReplyToTicketAsUserCommand>
{
    public ReplyToTicketAsUserValidator()
    {
        RuleFor(x => x.Body).Length(SupportTicket.MessageBodyMinLength, SupportTicket.MessageBodyMaxLength)
            .WithMessage($"Message must be between {SupportTicket.MessageBodyMinLength} and {SupportTicket.MessageBodyMaxLength} characters.");
    }
}

public sealed class ReplyToTicketAsUserHandler(
    ISupportTicketRepository ticketRepository,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<ReplyToTicketAsUserCommand, Result>
{
    public async Task<Result> Handle(ReplyToTicketAsUserCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var reporterUserId = executionContext.UserInfo.Id!;
        if (ticket.ReporterId != reporterUserId) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var now = timeProvider.GetUtcNow();
        var fromStatus = ticket.Status;
        ticket.PostUserMessage(reporterUserId, command.Body, command.Attachments, now);

        events.CollectEvent(new SupportTicketReplyPosted(ticket.Id, SupportMessageAuthorKind.User, command.Attachments.Length));
        if (fromStatus != ticket.Status)
        {
            events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, fromStatus, ticket.Status, SupportMessageAuthorKind.User));
        }

        if (command.MarkAsResolved && ticket.MarkResolvedByUser(now))
        {
            events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, SupportTicketStatus.AwaitingAgent, SupportTicketStatus.Resolved, SupportMessageAuthorKind.User));
        }

        ticketRepository.Update(ticket);
        return Result.Success();
    }
}
