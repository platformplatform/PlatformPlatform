using Bogus;
using PlatformPlatform.Main.Database;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.Main.Tests;

public sealed class DatabaseSeeder
{
    public readonly UserInfo Tenant1Member;
    public readonly UserInfo Tenant1Owner;
    public readonly TenantId TenantId;
    private readonly Faker _faker = new();

    public DatabaseSeeder(MainDbContext mainDbContext)
    {
        TenantId = TenantId.NewId();

        Tenant1Owner = new UserInfo
        {
            Email = "owner@tenant-1.com",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            Locale = "en-US",
            Role = "Owner",
            TenantId = TenantId
        };

        Tenant1Member = new UserInfo
        {
            Email = "member1@tenant-1.com",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            Locale = "en-US",
            Role = "Member",
            TenantId = TenantId
        };

        mainDbContext.SaveChanges();
    }
}
