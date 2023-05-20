namespace PlatformPlatform.AccountManagement.WebApi.Tenants;

public sealed record CreateTenantRequest(string Name, string Subdomain, string Email, string? Phone);