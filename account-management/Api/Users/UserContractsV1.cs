using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Users;

namespace PlatformPlatform.AccountManagement.Api.Users;

[UsedImplicitly]
public sealed record CreateUserRequest(string TenantId, string Email, UserRole UserRole);

public sealed record UpdateUserRequest(string Email, UserRole UserRole);

[UsedImplicitly]
public sealed record UserResponseDto(string Id, DateTime CreatedAt, DateTime? ModifiedAt, string Email,
    UserRole UserRole);