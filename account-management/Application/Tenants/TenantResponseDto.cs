using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

[UsedImplicitly]
public sealed record TenantResponseDto(string Id, DateTime CreatedAt, DateTime? ModifiedAt, string Name,
    TenantState State, string Email, string? Phone) : IIdentity
{
    public object GetId()
    {
        return Id;
    }
}