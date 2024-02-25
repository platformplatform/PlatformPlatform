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
        Tenant1 = Tenant.Create("tenant1", "Tenant 1", "1234567890", "user1@test.com");
        AccountRegistration1 = AccountRegistration.Create("newuser@test.com", "John", "Doe");
        AccountRegistration1.ConfirmEmail();
        accountManagementDbContext.AccountRegistrations.AddRange(AccountRegistration1);

        accountManagementDbContext.Tenants.AddRange(Tenant1);

        User1 = User.Create(Tenant1.Id, "user1@test.com", UserRole.TenantUser);
        accountManagementDbContext.Users.AddRange(User1);

        accountManagementDbContext.SaveChanges();
    }
}