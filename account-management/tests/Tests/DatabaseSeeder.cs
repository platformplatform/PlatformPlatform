using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public class DatabaseSeeder
{
    public const string Tenant1Name = "Tenant 1";
    public static readonly TenantId Tenant1Id = TenantId.NewId();

    private static readonly object Lock = new();
    private static bool _databaseIsSeeded;

    private readonly ApplicationDbContext _applicationDbContext;

    public DatabaseSeeder(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public void Seed()
    {
        lock (Lock)
        {
            if (_databaseIsSeeded) return;

            SeedTenants();

            _applicationDbContext.SaveChanges();

            _databaseIsSeeded = true;
        }
    }

    private void SeedTenants()
    {
        var tenant1 = new Tenant
        {
            Id = Tenant1Id, Name = Tenant1Name, Subdomain = "tenant1", Email = "foo@tenant1.com", Phone = "1234567890"
        };

        _applicationDbContext.Tenants.AddRange(tenant1);
    }
}