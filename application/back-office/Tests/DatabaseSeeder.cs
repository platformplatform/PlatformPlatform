using Bogus;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.BackOffice.Tests;

public sealed class DatabaseSeeder
{
    private readonly Faker _faker = new();
    public readonly UserInfo Tenant1Member;
    public readonly UserInfo Tenant1Owner;
    public readonly TenantId TenantId;

    public DatabaseSeeder(BackOfficeDbContext backOfficeDbContext)
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

        backOfficeDbContext.SaveChanges();
    }
}
