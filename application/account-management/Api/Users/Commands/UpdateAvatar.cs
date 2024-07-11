using System.Security.Cryptography;
using FluentValidation;
using PlatformPlatform.AccountManagement.Api.TelemetryEvents;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.Api.ApiResults;
using PlatformPlatform.SharedKernel.Api.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Application.Services;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

public sealed record UpdateAvatarCommand(UserId Id, Stream FileSteam, string ContentType)
    : ICommand, IRequest<Result>;

public sealed class UpdateAvatarEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users");

        // Id should be inferred from the authenticated user
        group.MapPost("/{id}/update-avatar", async Task<ApiResult> (UserId id, IFormFile file, ISender mediator)
            => await mediator.Send(new UpdateAvatarCommand(id, file.OpenReadStream(), file.ContentType))
        ).DisableAntiforgery(); // Disable antiforgery until we implement it
    }
}

public sealed class UpdateAvatarValidator : AbstractValidator<UpdateAvatarCommand>
{
    public UpdateAvatarValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(x => x == "image/jpeg")
            .WithMessage(_ => "Image must be of type Jpeg.");

        RuleFor(x => x.FileSteam.Length)
            .LessThanOrEqualTo(1024 * 1024)
            .WithMessage(_ => "Image must be less than 1MB.");
    }
}

public sealed class UpdateAvatarHandler(
    UserRepository userRepository,
    [FromKeyedServices("avatars-storage")] IBlobStorage blobStorage,
    ITelemetryEventsCollector events
) : IRequestHandler<UpdateAvatarCommand, Result>
{
    private const string ContainerName = "avatars";

    public async Task<Result> Handle(UpdateAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        var fileHash = await GetFileHash(command.FileSteam, cancellationToken);
        var blobName = $"{user.TenantId}/{user.Id}/{fileHash}.jpg";
        await blobStorage.UploadAsync(ContainerName, blobName, command.ContentType, command.FileSteam, cancellationToken);

        var avatarUrl = $"/{ContainerName}/{blobName}";
        user.UpdateAvatar(avatarUrl);

        userRepository.Update(user);

        events.CollectEvent(new UserAvatarUpdated(command.ContentType, command.FileSteam.Length));

        return Result.Success();
    }

    private async Task<string> GetFileHash(Stream fileStream, CancellationToken cancellationToken)
    {
        var hashBytes = await SHA1.Create().ComputeHashAsync(fileStream, cancellationToken);
        fileStream.Position = 0;
        // This just need to be unique for one user, who likely will ever have one avatar, so 16 chars should be enough
        return BitConverter.ToString(hashBytes).Replace("-", "")[..16].ToUpper();
    }
}
