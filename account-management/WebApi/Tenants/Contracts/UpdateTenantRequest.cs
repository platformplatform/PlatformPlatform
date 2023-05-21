namespace PlatformPlatform.AccountManagement.WebApi.Tenants.Contracts;

public sealed record UpdateTenantRequest(string Name, string Email, string? Phone);