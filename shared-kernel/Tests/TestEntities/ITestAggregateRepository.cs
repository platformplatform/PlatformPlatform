using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public interface ITestAggregateRepository : IRepository<TestAggregate, long>
{
}