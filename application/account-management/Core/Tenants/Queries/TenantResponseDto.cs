using PlatformPlatform.AccountManagement.Core.Tenants.Domain;

namespace PlatformPlatform.AccountManagement.Core.Tenants.Queries;

public sealed record TenantResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    TenantState State
);
