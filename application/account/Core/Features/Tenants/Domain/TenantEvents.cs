using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.DomainEvents;

namespace PlatformPlatform.Account.Features.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
