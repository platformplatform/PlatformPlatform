using System.Collections.Immutable;
using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.BackOffice.Commands;

[PublicAPI]
public sealed record PostInternalNoteCommand(string Body) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;

    public string Body { get; init; } = Body.Trim();

    [JsonIgnore] // Multipart bound separately
    public ImmutableArray<SupportMessageAttachment> Attachments { get; init; } = [];
}

public sealed class PostInternalNoteValidator : AbstractValidator<PostInternalNoteCommand>
{
    public PostInternalNoteValidator()
    {
        RuleFor(x => x.Body).Length(SupportTicket.MessageBodyMinLength, SupportTicket.MessageBodyMaxLength)
            .WithMessage($"Message must be between {SupportTicket.MessageBodyMinLength} and {SupportTicket.MessageBodyMaxLength} characters.");
    }
}

public sealed class PostInternalNoteHandler(
    ISupportTicketRepository ticketRepository,
    BackOfficeStaffContext backOfficeStaffContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<PostInternalNoteCommand, Result>
{
    public async Task<Result> Handle(PostInternalNoteCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var staff = backOfficeStaffContext.GetCurrent();
        ticket.PostStaffInternalNote(staff, command.Body, command.Attachments, timeProvider.GetUtcNow());

        ticketRepository.Update(ticket);
        events.CollectEvent(new SupportTicketReplyPosted(ticket.Id, SupportMessageAuthorKind.Internal, command.Attachments.Length));
        return Result.Success();
    }
}
