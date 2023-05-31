using PlatformPlatform.AccountManagement.Domain.Users;

namespace PlatformPlatform.AccountManagement.Api.Users.Contracts;

public sealed record UpdateUserRequest(string Email, UserRole UserRole);