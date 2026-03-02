using Microsoft.EntityFrameworkCore;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;

namespace SharedKernel.Tests.TestEntities;

public sealed class SoftDeletableTestDbContext(DbContextOptions<SoftDeletableTestDbContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : SharedKernelDbContext<SoftDeletableTestDbContext>(options, executionContext, timeProvider)
{
    public DbSet<SoftDeletableTestAggregate> SoftDeletableTestAggregates => Set<SoftDeletableTestAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.UseStringForEnums();
    }
}
