using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public class DatabaseSeeder
{
    public const string Tenant1Name = "Tenant 1";
    public static readonly TenantId Tenant1Id = TenantId.NewId();
    private readonly AccountManagementDbContext _accountManagementDbContext;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        _accountManagementDbContext = accountManagementDbContext;
        SeedTenants();
        accountManagementDbContext.SaveChanges();
    }

    private void SeedTenants()
    {
        var tenant1 = new Tenant(Tenant1Name, "tenant1", "foo@tenant1.com", "1234567890")
        {
            Id = Tenant1Id
        };

        _accountManagementDbContext.Tenants.AddRange(tenant1);
    }
}