using System.ComponentModel.DataAnnotations.Schema;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public sealed class SoftDeletableTestAggregate(string name) : AggregateRoot<long>(IdGenerator.NewId()), ISoftDeletable
{
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Name { get; set; } = name;

    public DateTimeOffset? DeletedAt { get; private set; }

    [NotMapped]
    public bool ForcePurge { get; private set; }

    public void MarkAsDeleted(DateTimeOffset deletedAt)
    {
        DeletedAt = deletedAt;
    }

    public void Restore()
    {
        DeletedAt = null;
    }

    public void MarkForPurge()
    {
        ForcePurge = true;
    }

    public static SoftDeletableTestAggregate Create(string name)
    {
        return new SoftDeletableTestAggregate(name);
    }
}
