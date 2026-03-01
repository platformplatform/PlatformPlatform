namespace PlatformPlatform.SharedKernel.Domain;

public abstract class SoftDeletableAggregateRoot<T>(T id) : AggregateRoot<T>(id), ISoftDeletable where T : IComparable<T>
{
    [UsedImplicitly]
    public DateTimeOffset? DeletedAt { get; private set; }
}
