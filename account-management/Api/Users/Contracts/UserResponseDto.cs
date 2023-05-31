using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Users;

namespace PlatformPlatform.AccountManagement.Api.Users.Contracts;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public sealed record UserResponseDto
{
    public required string Id { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime? ModifiedAt { get; init; }

    public required string Email { get; init; }

    public UserRole UserRole { get; init; }
}