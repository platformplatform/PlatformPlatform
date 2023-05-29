using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Contracts;

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