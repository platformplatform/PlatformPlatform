using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Api.Tenants;

[UsedImplicitly]
public sealed record CreateTenantRequest(string Name, string Subdomain, string Email, string? Phone);

public sealed record UpdateTenantRequest(string Name, string Email, string? Phone);

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public sealed record TenantResponseDto
{
    public required string Id { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime? ModifiedAt { get; init; }

    public required string Name { get; init; }

    public TenantState State { get; init; }

    public required string Email { get; init; }

    public string? Phone { get; init; }
}