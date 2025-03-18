using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;

namespace PlatformPlatform.AccountManagement.Tests;

public sealed class DatabaseSeeder
{
    public readonly Tenant Tenant1;
    public readonly User Tenant1Member;
    public readonly User Tenant1Owner;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        Tenant1 = Tenant.Create("owner@tenant-1.com");
        accountManagementDbContext.Set<Tenant>().AddRange(Tenant1);

        Tenant1Owner = User.Create(Tenant1.Id, "owner@tenant-1.com", UserRole.Owner, true, null);
        accountManagementDbContext.Set<User>().AddRange(Tenant1Owner);

        Tenant1Member = User.Create(Tenant1.Id, "member1@tenant-1.com", UserRole.Member, true, null);
        accountManagementDbContext.Set<User>().AddRange(Tenant1Owner);

        accountManagementDbContext.SaveChanges();
    }
}
