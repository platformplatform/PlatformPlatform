using SharedKernel.Persistence;

namespace SharedKernel.Tests.TestEntities;

public sealed class TestAggregateRepository(TestDbContext testDbContext)
    : RepositoryBase<TestAggregate, long>(testDbContext), ITestAggregateRepository;
