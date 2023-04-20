using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public class DatabaseSeeder
{
    public const string Tenant1Name = "Tenant 1";
    public const string Tenant2Name = "Tenant 2";
    public static readonly TenantId Tenant1Id = TenantId.NewId();
    public static readonly TenantId Tenant2Id = TenantId.NewId();

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
        var tenant1 = new Tenant {Id = Tenant1Id, CreatedAt = DateTime.UtcNow.Date, Name = Tenant1Name};
        var tenant2 = new Tenant {Id = Tenant2Id, CreatedAt = DateTime.UtcNow.Date, Name = Tenant2Name};

        _applicationDbContext.Tenants.AddRange(tenant1, tenant2);
    }
}