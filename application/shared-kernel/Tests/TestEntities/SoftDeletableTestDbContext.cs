using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

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
