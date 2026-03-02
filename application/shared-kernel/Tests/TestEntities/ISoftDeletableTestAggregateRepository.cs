using SharedKernel.Domain;

namespace SharedKernel.Tests.TestEntities;

public interface ISoftDeletableTestAggregateRepository : ICrudRepository<SoftDeletableTestAggregate, long>, ISoftDeletableRepository<SoftDeletableTestAggregate, long>;
