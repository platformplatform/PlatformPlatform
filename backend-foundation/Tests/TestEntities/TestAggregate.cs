using JetBrains.Annotations;
using PlatformPlatform.Foundation.DomainModeling.Entities;
using PlatformPlatform.Foundation.DomainModeling.Identity;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public sealed class TestAggregate : AggregateRoot<long>
{
    private TestAggregate(string name) : base(IdGenerator.NewId())
    {
        Name = name;
    }

    [UsedImplicitly]
    public string Name { get; set; }

    public static TestAggregate Create(string name)
    {
        var testAggregate = new TestAggregate(name);
        var testAggregateCreatedEvent = new TestAggregateCreatedEvent(testAggregate.Id, testAggregate.Name);
        testAggregate.AddDomainEvent(testAggregateCreatedEvent);
        return testAggregate;
    }
}