using System.Security.Cryptography;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record UpdateAvatarCommand(UserId Id, Stream FileSteam, string ContentType)
    : ICommand, IRequest<Result>;

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
    IUserRepository userRepository,
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
