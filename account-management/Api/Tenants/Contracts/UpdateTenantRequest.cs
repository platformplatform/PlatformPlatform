namespace PlatformPlatform.AccountManagement.Api.Tenants.Contracts;

public sealed record UpdateTenantRequest(string Name, string Email, string? Phone);