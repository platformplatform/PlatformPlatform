using PlatformPlatform.AccountManagement.Core.Users.Domain;

namespace PlatformPlatform.AccountManagement.Core.Users.Queries;

public sealed record UserResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Title,
    bool EmailConfirmed,
    string? AvatarUrl
);
