using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace SharedKernel.Tests.TestEntities;

public sealed class SoftDeletableTestAggregate(string name) : SoftDeletableAggregateRoot<long>(IdGenerator.NewId())
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; set; } = name;

    public static SoftDeletableTestAggregate Create(string name)
    {
        return new SoftDeletableTestAggregate(name);
    }
}
