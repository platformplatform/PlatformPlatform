using PlatformPlatform.SharedKernel.Domain.DomainEvents;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Domain;

public sealed record TenantCreatedEvent(TenantId TenantId, string Email) : IDomainEvent;
