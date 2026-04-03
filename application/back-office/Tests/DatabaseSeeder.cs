using BackOffice.Database;
using Bogus;
using SharedKernel.Authentication;
using SharedKernel.Domain;
using SharedKernel.Platform;

namespace BackOffice.Tests;

public sealed class DatabaseSeeder
{
    public readonly UserInfo ExternalUser;
    public readonly UserInfo Tenant1Member;
    public readonly UserInfo Tenant1Owner;
    public readonly TenantId TenantId;
    private readonly Faker _faker = new();

    public DatabaseSeeder(BackOfficeDbContext backOfficeDbContext)
    {
        TenantId = TenantId.NewId();

        Tenant1Owner = new UserInfo
        {
            Email = $"owner{Settings.Current.Identity.InternalEmailDomain}",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            IsInternalUser = true,
            Locale = "en-US",
            Role = "Owner",
            TenantId = TenantId
        };

        Tenant1Member = new UserInfo
        {
            Email = $"member1{Settings.Current.Identity.InternalEmailDomain}",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            IsInternalUser = true,
            Locale = "en-US",
            Role = "Member",
            TenantId = TenantId
        };

        ExternalUser = new UserInfo
        {
            Email = "external@tenant-1.com",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            IsInternalUser = false,
            Locale = "en-US",
            Role = "Member",
            TenantId = TenantId
        };

        backOfficeDbContext.SaveChanges();
    }
}
