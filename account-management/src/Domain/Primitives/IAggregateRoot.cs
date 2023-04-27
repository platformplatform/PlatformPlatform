namespace PlatformPlatform.AccountManagement.Domain.Primitives;

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
}