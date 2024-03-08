using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public sealed class DatabaseSeeder
{
    public readonly AccountRegistration AccountRegistration1;
    public readonly Tenant Tenant1;
    public readonly User User1;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        AccountRegistration1 = AccountRegistration.Create(new TenantId("newtenant"), "newuser@newtenant.com");
        accountManagementDbContext.AccountRegistrations.AddRange(AccountRegistration1);

        Tenant1 = Tenant.Create(new TenantId("tenant1"), "user1@test.com");
        accountManagementDbContext.Tenants.AddRange(Tenant1);

        User1 = User.Create(Tenant1.Id, "user1@test.com", UserRole.TenantUser, true);
        accountManagementDbContext.Users.AddRange(User1);

        accountManagementDbContext.SaveChanges();
    }
}