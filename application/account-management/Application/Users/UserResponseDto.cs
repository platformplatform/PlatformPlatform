namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record UserResponseDto(
    string Id,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    string Email,
    UserRole UserRole
);