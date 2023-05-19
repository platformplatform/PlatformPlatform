using PlatformPlatform.Foundation.DomainModeling.Persistence;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public interface ITestAggregateRepository : IRepository<TestAggregate, long>
{
}