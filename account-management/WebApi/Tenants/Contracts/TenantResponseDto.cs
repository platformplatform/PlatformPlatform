using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.WebApi.Tenants.Contracts;

/// <summary>
///     A shared DTO for used as return value for GET, POST and POST in <see cref="TenantEndpoints" />.
///     This class is returned by the WebAPI, making it a public contract, so it should be changed with care.
/// </summary>
public sealed record TenantResponseDto
{
    /// <summary>
    ///     The Id of the Tenant.
    /// </summary>
    [UsedImplicitly]
    public required string Id { get; init; }

    /// <summary>
    ///     The date and time when the Tenant was created in UTC format.
    /// </summary>
    [UsedImplicitly]
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    ///     The date and time when the Tenant was last modified in UTC format.
    /// </summary>
    [UsedImplicitly]
    public required DateTime? ModifiedAt { get; init; }

    /// <summary>
    ///     The name of the Tenant.
    /// </summary>
    [UsedImplicitly]
    public required string Name { get; init; }

    /// <summary>
    ///     The state of the Tenant (Trial, Active, Suspended).
    /// </summary>
    [UsedImplicitly]
    public TenantState State { get; init; }

    /// <summary>
    ///     The email of the tenant owner.
    /// </summary>
    [UsedImplicitly]
    public required string Email { get; init; }

    /// <summary>
    ///     The phone number of the tenant owner (optional).
    /// </summary>
    [UsedImplicitly]
    public string? Phone { get; init; }
}