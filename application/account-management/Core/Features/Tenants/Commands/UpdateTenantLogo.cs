using System.Security.Cryptography;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Integrations.BlobStorage;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record UpdateTenantLogoCommand(Stream FileStream, string ContentType) : ICommand, IRequest<Result>;

public sealed class UpdateTenantLogoValidator : AbstractValidator<UpdateTenantLogoCommand>
{
    public UpdateTenantLogoValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(x => x is "image/jpeg" or "image/png" or "image/gif" or "image/webp" or "image/svg+xml")
            .WithMessage(_ => "Image must be of type JPEG, PNG, GIF, WebP, or SVG.");

        RuleFor(x => x.FileStream.Length)
            .LessThanOrEqualTo(2 * 1024 * 1024)
            .WithMessage(_ => "Image must be smaller than 2 MB");
    }
}

public sealed class UpdateTenantLogoHandler(
    ITenantRepository tenantRepository,
    IExecutionContext executionContext,
    [FromKeyedServices("account-management-storage")]
    IBlobStorageClient blobStorageClient,
    ITelemetryEventsCollector events
)
    : IRequestHandler<UpdateTenantLogoCommand, Result>
{
    private const string ContainerName = "logos";

    public async Task<Result> Handle(UpdateTenantLogoCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to update tenant logo.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Result.Unauthorized("Tenant has been deleted.", responseHeaders: new Dictionary<string, string>
                {
                    { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
                }
            );
        }

        var fileHash = await GetFileHash(command.FileStream, cancellationToken);
        var fileExtension = GetFileExtension(command.ContentType);
        var blobName = $"{tenant.Id}/logo/{fileHash}.{fileExtension}";
        var logoUrl = $"/{ContainerName}/{blobName}";

        if (tenant.Logo.Url != logoUrl)
        {
            await blobStorageClient.UploadAsync(ContainerName, blobName, command.ContentType, command.FileStream, cancellationToken);

            tenant.UpdateLogo(logoUrl);
            tenantRepository.Update(tenant);

            events.CollectEvent(new TenantLogoUpdated(command.ContentType, command.FileStream.Length));
        }

        return Result.Success();
    }

    private static async Task<string> GetFileHash(Stream fileStream, CancellationToken cancellationToken)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = await sha1.ComputeHashAsync(fileStream, cancellationToken);
        fileStream.Position = 0;
        // This just needs to be unique for one tenant, who likely will ever only have one logo, so 16 chars should be enough
        return BitConverter.ToString(hashBytes).Replace("-", "")[..16].ToUpper();
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "image/svg+xml" => "svg",
            _ => throw new InvalidOperationException($"Unsupported content type: {contentType}")
        };
    }
}
