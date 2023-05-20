using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.WebApi.Tenants;

[UsedImplicitly]
public sealed record CreateTenantRequest(string Name, string Subdomain, string Email, string? Phone);