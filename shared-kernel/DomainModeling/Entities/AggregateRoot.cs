using System.ComponentModel.DataAnnotations.Schema;
using PlatformPlatform.Foundation.DomainModeling.DomainEvents;

namespace PlatformPlatform.Foundation.DomainModeling.Entities;

/// <summary>
///     Interface for aggregate roots, which also implements IAuditableEntity. Aggregate roots are a concept in
///     Domain-Driven Design (DDD). An aggregate is an entity, but only some entities are aggregates. For example, an
///     Order in an e-commerce system is an aggregate, but an OrderLine is not. An aggregate is a cluster of associated
///     objects that are treated as a unit.
///     In DDD, Repositories are used to read and write aggregates in the database. For example, when an aggregate is
///     deleted, all entities belonging to the aggregate are deleted as well. Also, only aggregates can be fetched from
///     the database, while entities that are not aggregates cannot (fetch the aggregate to get access to the entities).
/// </summary>
public interface IAggregateRoot : IAuditableEntity
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}

public abstract class AggregateRoot<T> : AudibleEntity<T>, IAggregateRoot where T : IComparable<T>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(T id) : base(id)
    {
    }

    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}