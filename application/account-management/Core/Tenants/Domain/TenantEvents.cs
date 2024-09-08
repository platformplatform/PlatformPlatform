using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.DomainEvents;

namespace PlatformPlatform.AccountManagement.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
