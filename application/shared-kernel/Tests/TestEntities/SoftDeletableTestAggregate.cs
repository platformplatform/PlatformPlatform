using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class SoftDeletableTestAggregate(string name) : SoftDeletableAggregateRoot<long>(IdGenerator.NewId())
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; set; } = name;

    public static SoftDeletableTestAggregate Create(string name)
    {
        return new SoftDeletableTestAggregate(name);
    }
}
