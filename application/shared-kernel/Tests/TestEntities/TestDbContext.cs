using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options)
    : SharedKernelDbContext<TestDbContext>(options)
{
    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());
        
        base.OnConfiguring(optionsBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.UseStringForEnums();
    }
}
