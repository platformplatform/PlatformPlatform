namespace PlatformPlatform.SharedKernel.Domain;

public interface ITenantScopedEntity
{
    TenantId TenantId { get; }
}
