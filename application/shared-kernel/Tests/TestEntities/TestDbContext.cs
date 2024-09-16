using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, IExecutionContext executionContext)
    : SharedKernelDbContext<TestDbContext>(options, executionContext)
{
    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseStringForEnums();
    }
}
