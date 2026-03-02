using SharedKernel.Domain;
using SharedKernel.DomainEvents;

namespace Account.Features.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
