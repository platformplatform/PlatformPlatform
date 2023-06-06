using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

[UsedImplicitly]
public sealed record TenantResponseDto(string Id, DateTime CreatedAt, DateTime? ModifiedAt, string Name,
    TenantState State, string Email, string? Phone);