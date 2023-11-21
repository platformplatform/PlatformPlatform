using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class TestAggregate(string name) : AggregateRoot<long>(IdGenerator.NewId())
{
    public string Name { get; set; } = name;

    public static TestAggregate Create(string name)
    {
        var testAggregate = new TestAggregate(name);
        var testAggregateCreatedEvent = new TestAggregateCreatedEvent(testAggregate.Id, testAggregate.Name);
        testAggregate.AddDomainEvent(testAggregateCreatedEvent);
        return testAggregate;
    }
}