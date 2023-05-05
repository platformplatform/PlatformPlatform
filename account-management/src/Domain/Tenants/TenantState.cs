using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public enum TenantState
{
    Trial,

    [UsedImplicitly]
    Active,

    [UsedImplicitly]
    Suspended
}