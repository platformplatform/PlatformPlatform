using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public class DatabaseSeeder
{
    public const string Tenant1Name = "Tenant 1";
    public const string User1Email = "user1@test.com";
    public static readonly TenantId Tenant1Id = new("tenant1");
    public static readonly UserId User1Id = UserId.NewId();
    private readonly AccountManagementDbContext _accountManagementDbContext;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        _accountManagementDbContext = accountManagementDbContext;
        SeedTenants();
        SeedUsers();
        accountManagementDbContext.SaveChanges();
    }

    private void SeedTenants()
    {
        var tenant1 = new Tenant(Tenant1Id, Tenant1Name, "1234567890");

        _accountManagementDbContext.Tenants.AddRange(tenant1);
    }

    private void SeedUsers()
    {
        var user1 = new User(Tenant1Id, User1Email, UserRole.TenantUser)
        {
            Id = User1Id
        };

        _accountManagementDbContext.Users.AddRange(user1);
    }
}