using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

[UsedImplicitly]
public sealed record TenantCreatedEvent(TenantId TenantId, string Name) : IDomainEvent;