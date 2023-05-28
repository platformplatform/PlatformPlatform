using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public class TestAggregateRepository : RepositoryBase<TestAggregate, long>, ITestAggregateRepository
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public TestAggregateRepository(TestDbContext testDbContext) : base(testDbContext)
    {
    }
}