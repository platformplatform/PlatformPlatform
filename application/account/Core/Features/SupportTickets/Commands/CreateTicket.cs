using System.Collections.Immutable;
using Account.Features.SupportTickets.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.SupportTickets.Commands;

[PublicAPI]
public sealed record CreateTicketCommand(string Subject, string Body, SupportTicketCategory Category) : ICommand, IRequest<Result<SupportTicketId>>
{
    public string Subject { get; init; } = Subject.Trim();

    public string Body { get; init; } = Body.Trim();

    [JsonIgnore] // Multipart bound separately
    public ImmutableArray<SupportMessageAttachment> Attachments { get; init; } = [];
}

public sealed class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Subject).Length(SupportTicket.SubjectMinLength, SupportTicket.SubjectMaxLength)
            .WithMessage($"Subject must be between {SupportTicket.SubjectMinLength} and {SupportTicket.SubjectMaxLength} characters.");
        RuleFor(x => x.Body).Length(SupportTicket.MessageBodyMinLength, SupportTicket.MessageBodyMaxLength)
            .WithMessage($"Message must be between {SupportTicket.MessageBodyMinLength} and {SupportTicket.MessageBodyMaxLength} characters.");
    }
}

public sealed class CreateTicketHandler(
    ISupportTicketRepository ticketRepository,
    IUserRepository userRepository,
    IExecutionContext executionContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<CreateTicketCommand, Result<SupportTicketId>>
{
    public async Task<Result<SupportTicketId>> Handle(CreateTicketCommand command, CancellationToken cancellationToken)
    {
        var reporter = await userRepository.GetLoggedInUserAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var ticket = SupportTicket.Create(
            executionContext.TenantId!,
            reporter.Id,
            reporter.Role.ToString(),
            reporter.Email,
            command.Subject,
            command.Category,
            now
        );
        ticket.PostUserMessage(reporter.Id, command.Body, command.Attachments, now);

        await ticketRepository.AddAsync(ticket, cancellationToken);

        events.CollectEvent(new SupportTicketCreated(ticket.Id, command.Category, command.Attachments.Length));

        return ticket.Id;
    }
}
