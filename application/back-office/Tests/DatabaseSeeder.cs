using Bogus;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.BackOffice.Tests;

public sealed class DatabaseSeeder
{
    private readonly Faker _faker = new();

    public DatabaseSeeder(BackOfficeDbContext backOfficeDbContext)
    {
        OwnerUser = new UserInfo
        {
            Email = "owner@tenant-1.com",
            FirstName = _faker.Person.FirstName,
            LastName = _faker.Person.LastName,
            Id = UserId.NewId(),
            IsAuthenticated = true,
            Locale = "en-US",
            Role = "Owner",
            TenantId = TenantId.NewId()
        };

        backOfficeDbContext.SaveChanges();
    }

    public UserInfo OwnerUser { get; set; }
}
