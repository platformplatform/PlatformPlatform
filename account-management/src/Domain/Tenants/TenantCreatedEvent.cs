using JetBrains.Annotations;
using PlatformPlatform.Foundation.DddCore;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

[UsedImplicitly]
public sealed record TenantCreatedEvent(TenantId TenantId, string Name) : IDomainEvent;