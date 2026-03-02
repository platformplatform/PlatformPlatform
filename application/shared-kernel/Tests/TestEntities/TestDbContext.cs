using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;

namespace SharedKernel.Tests.TestEntities;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<TestDbContext>(options, executionContext, timeProvider)
{
    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseStringForEnums();

        modelBuilder.Entity<TestAggregate>(entity => { entity.MapStronglyTypedString(e => e.ExternalId); });
    }
}
