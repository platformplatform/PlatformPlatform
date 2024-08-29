using PlatformPlatform.BackOffice.Core.Database;

namespace PlatformPlatform.BackOffice.Tests;

public sealed class DatabaseSeeder
{
    public DatabaseSeeder(BackOfficeDbContext backOfficeDbContext)
    {
        backOfficeDbContext.SaveChanges();
    }
}
