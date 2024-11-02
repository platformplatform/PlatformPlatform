using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.DomainEvents;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
