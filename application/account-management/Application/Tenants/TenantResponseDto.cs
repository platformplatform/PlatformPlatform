namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record TenantResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Name,
    TenantState State
);
