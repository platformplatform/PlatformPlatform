namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record UserResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole UserRole,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    string? AvatarUrl
);
