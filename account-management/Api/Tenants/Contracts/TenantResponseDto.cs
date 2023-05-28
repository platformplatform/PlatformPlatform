using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Contracts;

public sealed record TenantResponseDto
{
    [UsedImplicitly]
    public required string Id { get; init; }

    [UsedImplicitly]
    public required DateTime CreatedAt { get; init; }

    [UsedImplicitly]
    public required DateTime? ModifiedAt { get; init; }

    [UsedImplicitly]
    public required string Name { get; init; }

    [UsedImplicitly]
    public TenantState State { get; init; }

    [UsedImplicitly]
    public required string Email { get; init; }

    [UsedImplicitly]
    public string? Phone { get; init; }
}