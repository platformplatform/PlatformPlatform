using Account.Features.SupportTickets.Commands;
using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Queries;
using Account.Features.SupportTickets.Shared;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.ApiResults;
using SharedKernel.Cqrs;
using SharedKernel.Endpoints;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.BlobStorage;
using SharedKernel.OpenApi;

namespace Account.Api.Endpoints;

public sealed class SupportTicketEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/support-tickets";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("SupportTickets").WithGroupName(OpenApiDocumentNames.Account).RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<MyTicketsResponse>> ([AsParameters] GetMyTicketsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<MyTicketsResponse>();

        group.MapGet("/{id}", async Task<ApiResult<TicketDetailResponse>> (SupportTicketId id, IMediator mediator)
            => await mediator.Send(new GetTicketDetailQuery(id))
        ).Produces<TicketDetailResponse>();

        group.MapGet("/{id}/messages/{messageId}/attachments/{**fileName}", async (SupportTicketId id, SupportMessageId messageId, string fileName, ISupportTicketRepository ticketRepository, IExecutionContext executionContext, [FromKeyedServices("account-storage")] IBlobStorageClient blobStorageClient, HttpContext httpContext, CancellationToken cancellationToken)
            => await SupportTicketAttachmentEndpoint.DownloadForReporterAsync(id, messageId, fileName, ticketRepository, executionContext, blobStorageClient, httpContext, cancellationToken)
        );

        group.MapPost("/", async Task<ApiResult<SupportTicketId>> ([FromForm] string subject, [FromForm] string body, [FromForm] SupportTicketCategory category, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, IExecutionContext executionContext, CancellationToken cancellationToken)
            => await HandleCreateAsync(subject, body, category, files, mediator, uploader, executionContext, cancellationToken)
        ).Produces<SupportTicketId>().DisableAntiforgery();

        group.MapPost("/{id}/reply", async Task<ApiResult> (SupportTicketId id, [FromForm] string body, [FromForm] bool markAsResolved, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, IExecutionContext executionContext, CancellationToken cancellationToken)
            => await HandleReplyAsync(id, body, markAsResolved, files, mediator, uploader, executionContext, cancellationToken)
        ).DisableAntiforgery();

        group.MapPost("/{id}/mark-resolved", async Task<ApiResult> (SupportTicketId id, IMediator mediator)
            => await mediator.Send(new MarkResolvedByUserCommand { Id = id })
        );

        group.MapPost("/{id}/close", async Task<ApiResult> (SupportTicketId id, CloseTicketByUserCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapPost("/{id}/reopen", async Task<ApiResult> (SupportTicketId id, IMediator mediator)
            => await mediator.Send(new ReopenTicketCommand { Id = id })
        );

        group.MapPost("/{id}/csat", async Task<ApiResult> (SupportTicketId id, SubmitCsatCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );
    }

    private static async Task<ApiResult<SupportTicketId>> HandleCreateAsync(string subject, string body, SupportTicketCategory category, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, IExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var uploadResult = await uploader.UploadTenantAttachmentsAsync(executionContext.TenantId!, files, cancellationToken);
        if (!uploadResult.IsSuccess) return Result<SupportTicketId>.From(uploadResult);
        return await mediator.Send(new CreateTicketCommand(subject, body, category) { Attachments = uploadResult.Value }, cancellationToken);
    }

    private static async Task<ApiResult> HandleReplyAsync(SupportTicketId id, string body, bool markAsResolved, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, IExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var uploadResult = await uploader.UploadTenantAttachmentsAsync(executionContext.TenantId!, files, cancellationToken);
        if (!uploadResult.IsSuccess) return Result.From(uploadResult);
        return await mediator.Send(new ReplyToTicketAsUserCommand(body) { Id = id, Attachments = uploadResult.Value, MarkAsResolved = markAsResolved }, cancellationToken);
    }
}
