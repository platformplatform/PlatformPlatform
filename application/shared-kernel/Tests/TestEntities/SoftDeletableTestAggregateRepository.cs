using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class SoftDeletableTestAggregateRepository(SoftDeletableTestDbContext testDbContext)
    : SoftDeletableRepositoryBase<SoftDeletableTestAggregate, long>(testDbContext), ISoftDeletableTestAggregateRepository;
