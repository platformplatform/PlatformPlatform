namespace PlatformPlatform.AccountManagement.WebApi.Tenants;

public sealed record UpdateTenantRequest(string Name, string Email, string? Phone);