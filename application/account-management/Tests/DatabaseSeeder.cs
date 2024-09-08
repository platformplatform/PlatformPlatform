using Bogus;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Signups.Domain;
using PlatformPlatform.AccountManagement.Tenants.Domain;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Tests;

public sealed class DatabaseSeeder
{
    private readonly Faker _faker = new();
    public readonly string OneTimePassword;
    public readonly Signup Signup1;
    public readonly Tenant Tenant1;
    public readonly Tenant TenantForSearching;
    public readonly User User1;
    public readonly User User1ForSearching;
    public readonly User User2ForSearching;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        OneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePassword);

        Signup1 = Signup.Create(new TenantId(_faker.Subdomain()), _faker.Internet.Email(), oneTimePasswordHash);

        accountManagementDbContext.Signups.AddRange(Signup1);

        Tenant1 = Tenant.Create(new TenantId(_faker.Subdomain()), _faker.Internet.Email());
        accountManagementDbContext.Tenants.AddRange(Tenant1);

        User1 = User.Create(Tenant1.Id, _faker.Internet.Email(), UserRole.Owner, true, null);
        accountManagementDbContext.Users.AddRange(User1);

        TenantForSearching = Tenant.Create(new TenantId(_faker.Subdomain()), _faker.Internet.Email());
        accountManagementDbContext.Tenants.AddRange(TenantForSearching);

        User1ForSearching = User.Create(TenantForSearching.Id, "willgates@email.com", UserRole.Member, true, null);
        User1ForSearching.Update("William Henry", "Gates", "Philanthropist & Innovator");

        User2ForSearching = User.Create(TenantForSearching.Id, _faker.Internet.Email(), UserRole.Owner, true, null);

        accountManagementDbContext.Users.AddRange(User1);
        accountManagementDbContext.Users.AddRange(User1ForSearching);
        accountManagementDbContext.Users.AddRange(User2ForSearching);

        accountManagementDbContext.SaveChanges();
    }
}
