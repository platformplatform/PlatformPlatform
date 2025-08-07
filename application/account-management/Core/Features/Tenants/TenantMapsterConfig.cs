using Mapster;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Queries;

namespace PlatformPlatform.AccountManagement.Features.Tenants;

public static class TenantMapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<Tenant, TenantResponse>
            .NewConfig()
            .Map(dest => dest.LogoUrl, src => src.Logo.Url);
    }
}
