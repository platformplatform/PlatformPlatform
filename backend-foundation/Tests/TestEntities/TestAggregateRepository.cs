using PlatformPlatform.Foundation.PersistenceInfrastructure.Persistence;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public class TestAggregateRepository : RepositoryBase<TestAggregate, long>, ITestAggregateRepository
{
    // ReSharper disable once SuggestBaseTypeForParameterInConstructor
    public TestAggregateRepository(TestDbContext testDbContext) : base(testDbContext)
    {
    }
}