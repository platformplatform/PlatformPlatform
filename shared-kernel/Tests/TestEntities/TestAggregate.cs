using PlatformPlatform.SharedKernel.DomainModeling.Entities;
using PlatformPlatform.SharedKernel.DomainModeling.Identity;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class TestAggregate : AggregateRoot<long>
{
    private TestAggregate(string name) : base(IdGenerator.NewId())
    {
        Name = name;
    }

    public string Name { get; set; }

    public static TestAggregate Create(string name)
    {
        var testAggregate = new TestAggregate(name);
        var testAggregateCreatedEvent = new TestAggregateCreatedEvent(testAggregate.Id, testAggregate.Name);
        testAggregate.AddDomainEvent(testAggregateCreatedEvent);
        return testAggregate;
    }
}