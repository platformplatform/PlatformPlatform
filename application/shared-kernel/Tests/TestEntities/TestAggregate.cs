using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public enum TestStatus
{
    Pending,
    Active,
    Completed
}

[IdPrefix("ext")]
public sealed record ExternalId(string Value) : StronglyTypedString<ExternalId>(Value);

public sealed class TestAggregate(string name) : AggregateRoot<long>(IdGenerator.NewId())
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; set; } = name;

    public TestStatus Status { get; set; } = TestStatus.Pending;

    public TestStatus? NullableStatus { get; set; }

    public ExternalId ExternalId { get; set; } = ExternalId.NewId("ext_default");

    public static TestAggregate Create(string name)
    {
        var testAggregate = new TestAggregate(name);
        var testAggregateCreatedEvent = new TestAggregateCreatedEvent(testAggregate.Id, testAggregate.Name);
        testAggregate.AddDomainEvent(testAggregateCreatedEvent);
        return testAggregate;
    }
}
