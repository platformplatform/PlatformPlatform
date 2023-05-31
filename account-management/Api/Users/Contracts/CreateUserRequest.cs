using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Users;

namespace PlatformPlatform.AccountManagement.Api.Users.Contracts;

[UsedImplicitly]
public sealed record CreateUserRequest(string Email, UserRole UserRole);