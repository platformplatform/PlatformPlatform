namespace PlatformPlatform.AccountManagement.Domain.Primitives;

/// <summary>
///     Interface for an aggregate root, which also implements IAuditableEntity.
///     Aggregate roots are a DDD concept. An Aggregate is an entity but only some entities are aggregates.
///     In DDD Repositories are used for read and write Aggregates to the database. E.g. when an aggregate
///     is deleted all entities belong to the aggregate are deleted as well.
/// </summary>
public interface IAggregateRoot : IAuditableEntity
{
}