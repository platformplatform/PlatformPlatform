using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public class TestAggregateRepository(TestDbContext testDbContext)
    : RepositoryBase<TestAggregate, long>(testDbContext), ITestAggregateRepository;