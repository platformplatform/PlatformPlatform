using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Users;

namespace PlatformPlatform.AccountManagement.Api.Users;

[UsedImplicitly]
public sealed record UserResponseDto(string Id, DateTime CreatedAt, DateTime? ModifiedAt, string Email,
    UserRole UserRole);