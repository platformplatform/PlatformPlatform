namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record UserResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole UserRole,
    string FirstName,
    string LastName,
    bool EmailConfirmed
);