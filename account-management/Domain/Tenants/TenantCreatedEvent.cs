using PlatformPlatform.SharedKernel.DomainModeling.DomainEvents;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantCreatedEvent(TenantId TenantId) : IDomainEvent;