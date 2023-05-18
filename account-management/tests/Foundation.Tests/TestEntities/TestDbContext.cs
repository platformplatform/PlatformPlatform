using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Infrastructure;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public class TestDbContext : FoundationDbContext<TestDbContext>
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseStringForEnums();
    }
}