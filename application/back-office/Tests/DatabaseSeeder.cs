using PlatformPlatform.BackOffice.Infrastructure;

namespace PlatformPlatform.BackOffice.Tests;

public sealed class DatabaseSeeder
{
    public DatabaseSeeder(BackOfficeDbContext backOfficeDbContext)
    {
        backOfficeDbContext.SaveChanges();
    }
}
