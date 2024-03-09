using Bogus;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Application.AccountRegistrations;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.AccountManagement.Infrastructure;

namespace PlatformPlatform.AccountManagement.Tests;

public sealed class DatabaseSeeder
{
    private readonly Faker _faker = new();
    public readonly AccountRegistration AccountRegistration1;
    public readonly string OneTimePassword;
    public readonly Tenant Tenant1;
    public readonly User User1;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        OneTimePassword = StartAccountRegistrationCommandHandler.GenerateOneTimePassword(6);
        var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePassword);

        AccountRegistration1 =
            AccountRegistration.Create(new TenantId(_faker.Subdomain()), _faker.Internet.Email(), oneTimePasswordHash);

        accountManagementDbContext.AccountRegistrations.AddRange(AccountRegistration1);

        Tenant1 = Tenant.Create(new TenantId(_faker.Subdomain()), _faker.Internet.Email());
        accountManagementDbContext.Tenants.AddRange(Tenant1);

        User1 = User.Create(Tenant1.Id, _faker.Internet.Email(), UserRole.TenantOwner, true);
        accountManagementDbContext.Users.AddRange(User1);

        accountManagementDbContext.SaveChanges();
    }
}