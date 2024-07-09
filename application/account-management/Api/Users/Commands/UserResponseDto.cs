using PlatformPlatform.AccountManagement.Api.Users.Domain;

namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

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
