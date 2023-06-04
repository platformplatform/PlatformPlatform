using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

[UsedImplicitly]
public sealed record CreateTenantRequest(string Name, string Subdomain, string Email, string? Phone);

public sealed record UpdateTenantRequest(string Name, string Email, string? Phone);

[UsedImplicitly]
public sealed record TenantResponseDto(string Id, DateTime CreatedAt, DateTime? ModifiedAt, string Name,
    TenantState State, string Email, string? Phone);