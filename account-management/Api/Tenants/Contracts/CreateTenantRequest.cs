using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Contracts;

[UsedImplicitly]
public sealed record CreateTenantRequest(string Name, string Subdomain, string Email, string? Phone);