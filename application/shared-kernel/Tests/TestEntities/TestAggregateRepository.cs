using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class TestAggregateRepository(TestDbContext testDbContext)
    : RepositoryBase<TestAggregate, long>(testDbContext), ITestAggregateRepository;