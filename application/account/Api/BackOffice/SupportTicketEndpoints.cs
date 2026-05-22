using Account.Features.SupportTickets.BackOffice.Commands;
using Account.Features.SupportTickets.BackOffice.Queries;
using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SharedKernel.ApiResults;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Cqrs;
using SharedKernel.Endpoints;
using SharedKernel.Integrations.BlobStorage;
using SharedKernel.OpenApi;

namespace Account.Api.BackOffice;

public sealed class SupportTicketEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/back-office/support-tickets";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var backOfficeHost = routes.ServiceProvider.GetRequiredService<IOptions<BackOfficeHostOptions>>().Value.Host;

        var group = routes.MapGroup(RoutesPrefix)
            .WithTags("BackOfficeSupportTickets")
            .WithGroupName(OpenApiDocumentNames.BackOffice)
            .RequireHost(backOfficeHost)
            .RequireAuthorization(BackOfficeIdentityDefaults.PolicyName)
            .ProducesValidationProblem();

        group.MapGet("/", async Task<ApiResult<AllTicketsResponse>> ([AsParameters] GetAllTicketsQuery query, IMediator mediator)
            => await mediator.Send(query)
        ).Produces<AllTicketsResponse>();

        group.MapGet("/{id}", async Task<ApiResult<StaffTicketDetailResponse>> (SupportTicketId id, IMediator mediator)
            => await mediator.Send(new GetTicketDetailForStaffQuery(id))
        ).Produces<StaffTicketDetailResponse>();

        group.MapGet("/{id}/messages/{messageId}/attachments/{**fileName}", async (SupportTicketId id, SupportMessageId messageId, string fileName, ISupportTicketRepository ticketRepository, [FromKeyedServices("account-storage")] IBlobStorageClient blobStorageClient, HttpContext httpContext, CancellationToken cancellationToken)
            => await SupportTicketAttachmentEndpoint.DownloadForStaffAsync(id, messageId, fileName, ticketRepository, blobStorageClient, httpContext, cancellationToken)
        );

        group.MapPost("/{id}/reply", async Task<ApiResult> (SupportTicketId id, [FromForm] string body, [FromForm] bool markAsResolved, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, CancellationToken cancellationToken)
            => await HandleStaffReplyAsync(id, body, markAsResolved, files, mediator, uploader, cancellationToken)
        ).DisableAntiforgery();

        group.MapPost("/{id}/internal-note", async Task<ApiResult> (SupportTicketId id, [FromForm] string body, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, CancellationToken cancellationToken)
            => await HandleInternalNoteAsync(id, body, files, mediator, uploader, cancellationToken)
        ).DisableAntiforgery();

        group.MapPut("/{id}/assignee", async Task<ApiResult> (SupportTicketId id, AssignTicketCommand command, IMediator mediator)
            => await mediator.Send(command with { Id = id })
        );

        group.MapPost("/{id}/mark-resolved", async Task<ApiResult> (SupportTicketId id, IMediator mediator)
            => await mediator.Send(new MarkResolvedByStaffCommand { Id = id })
        );
    }

    private static async Task<ApiResult> HandleStaffReplyAsync(SupportTicketId id, string body, bool markAsResolved, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, CancellationToken cancellationToken)
    {
        var uploadResult = await uploader.UploadStaffAttachmentsAsync(files, cancellationToken);
        if (!uploadResult.IsSuccess) return Result.From(uploadResult);
        return await mediator.Send(new ReplyToTicketAsStaffCommand(body) { Id = id, Attachments = uploadResult.Value, MarkAsResolved = markAsResolved }, cancellationToken);
    }

    private static async Task<ApiResult> HandleInternalNoteAsync(SupportTicketId id, string body, IFormFileCollection files, IMediator mediator, SupportAttachmentUploader uploader, CancellationToken cancellationToken)
    {
        var uploadResult = await uploader.UploadStaffAttachmentsAsync(files, cancellationToken);
        if (!uploadResult.IsSuccess) return Result.From(uploadResult);
        return await mediator.Send(new PostInternalNoteCommand(body) { Id = id, Attachments = uploadResult.Value }, cancellationToken);
    }
}
