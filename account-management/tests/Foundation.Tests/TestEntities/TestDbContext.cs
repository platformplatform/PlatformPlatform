using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Infrastructure;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        DbContextConfiguration.ConfigureOnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DbContextConfiguration.ConfigureOnConfiguring(optionsBuilder);
    }
}

public static class DbContextConfiguration
{
    public static void ConfigureOnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseStringForEnums();
    }

    public static void ConfigureOnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());
    }
}