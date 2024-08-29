using PlatformPlatform.SharedKernel.DomainEvents;

namespace PlatformPlatform.AccountManagement.Core.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
