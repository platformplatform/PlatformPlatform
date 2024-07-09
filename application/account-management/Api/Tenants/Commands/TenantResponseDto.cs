using PlatformPlatform.AccountManagement.Api.Tenants.Domain;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Commands;

public sealed record TenantResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    TenantState State
);
