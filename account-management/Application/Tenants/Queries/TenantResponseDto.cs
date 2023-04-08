using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Queries;

public record TenantResponseDto
{
    public required long Id { get; init; }

    public required string Name { get; init; }

    public static TenantResponseDto CreateFrom(Tenant tenant)
    {
        return new TenantResponseDto {Id = tenant.Id, Name = tenant.Name};
    }
}