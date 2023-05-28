using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantCreatedEvent(TenantId TenantId) : IDomainEvent;