using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public interface ISoftDeletableTestAggregateRepository : ICrudRepository<SoftDeletableTestAggregate, long>, ISoftDeletableRepository<SoftDeletableTestAggregate, long>;
