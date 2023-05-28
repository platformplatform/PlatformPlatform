using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public class TestAggregateRepository : RepositoryBase<TestAggregate, long>, ITestAggregateRepository
{
    public TestAggregateRepository(TestDbContext testDbContext) : base(testDbContext)
    {
    }
}