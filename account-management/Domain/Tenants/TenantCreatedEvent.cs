using JetBrains.Annotations;
using PlatformPlatform.Foundation.DomainModeling.DomainEvents;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantCreatedEvent(TenantId TenantId) : IDomainEvent;