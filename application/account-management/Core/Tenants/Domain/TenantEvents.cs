using PlatformPlatform.SharedKernel.DomainEvents;
using PlatformPlatform.SharedKernel.Entities;

namespace PlatformPlatform.AccountManagement.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
