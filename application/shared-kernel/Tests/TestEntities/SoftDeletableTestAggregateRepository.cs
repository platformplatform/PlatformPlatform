using SharedKernel.Persistence;

namespace SharedKernel.Tests.TestEntities;

public sealed class SoftDeletableTestAggregateRepository(SoftDeletableTestDbContext testDbContext)
    : SoftDeletableRepositoryBase<SoftDeletableTestAggregate, long>(testDbContext), ISoftDeletableTestAggregateRepository;
