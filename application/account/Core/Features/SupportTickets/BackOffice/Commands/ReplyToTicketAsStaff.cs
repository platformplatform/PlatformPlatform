using System.Collections.Immutable;
using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.EmailTemplates;
using Account.Features.SupportTickets.Shared;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Emails;
using SharedKernel.Integrations.Email;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.BackOffice.Commands;

[PublicAPI]
public sealed record ReplyToTicketAsStaffCommand(string Body) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public SupportTicketId Id { get; init; } = null!;

    public string Body { get; init; } = Body.Trim();

    [JsonIgnore] // Multipart bound separately
    public ImmutableArray<SupportMessageAttachment> Attachments { get; init; } = [];

    public bool MarkAsResolved { get; init; }
}

public sealed class ReplyToTicketAsStaffValidator : AbstractValidator<ReplyToTicketAsStaffCommand>
{
    public ReplyToTicketAsStaffValidator()
    {
        RuleFor(x => x.Body).Length(SupportTicket.MessageBodyMinLength, SupportTicket.MessageBodyMaxLength)
            .WithMessage($"Message must be between {SupportTicket.MessageBodyMinLength} and {SupportTicket.MessageBodyMaxLength} characters.");
    }
}

public sealed class ReplyToTicketAsStaffHandler(
    ISupportTicketRepository ticketRepository,
    IUserRepository userRepository,
    BackOfficeStaffContext backOfficeStaffContext,
    IEmailRenderer emailRenderer,
    IEmailClient emailClient,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<ReplyToTicketAsStaffCommand, Result>
{
    public async Task<Result> Handle(ReplyToTicketAsStaffCommand command, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(command.Id, cancellationToken);
        if (ticket is null) return Result.NotFound($"Support ticket with id '{command.Id}' not found.");

        var staff = backOfficeStaffContext.GetCurrent();
        var now = timeProvider.GetUtcNow();
        var fromStatus = ticket.Status;

        ticket.PostStaffPublicMessage(staff, command.Body, command.Attachments, now);
        events.CollectEvent(new SupportTicketReplyPosted(ticket.Id, SupportMessageAuthorKind.Staff, command.Attachments.Length));
        if (fromStatus != ticket.Status)
        {
            events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, fromStatus, ticket.Status, SupportMessageAuthorKind.Staff));
        }

        if (command.MarkAsResolved && ticket.MarkResolvedByStaff(staff, now))
        {
            events.CollectEvent(new SupportTicketStatusChanged(ticket.Id, SupportTicketStatus.AwaitingUser, SupportTicketStatus.Resolved, SupportMessageAuthorKind.Staff));
        }

        ticketRepository.Update(ticket);

        var reporter = (await userRepository.GetByIdsUnfilteredAsync([ticket.ReporterId], cancellationToken)).SingleOrDefault();
        var locale = reporter?.Locale is { Length: > 0 } loc ? loc : "en-US";
        var ticketUrl = $"{Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)}/support/tickets/{ticket.Id}";

        var template = new SupportStaffReplyEmailTemplate(
            locale,
            new SupportStaffReplyEmailModel(staff.DisplayName, ticket.ShortDisplayId, ticket.Subject, command.Body, ticketUrl)
        );
        var rendered = emailRenderer.RenderEmail(template);
        await emailClient.SendAsync(
            new EmailMessage(ticket.ReporterEmailSnapshot, rendered.Subject, rendered.HtmlBody, rendered.PlainTextBody),
            cancellationToken
        );

        return Result.Success();
    }
}
