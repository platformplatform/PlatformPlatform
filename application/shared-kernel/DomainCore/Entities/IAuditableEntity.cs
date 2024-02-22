namespace PlatformPlatform.SharedKernel.DomainCore.Entities;

/// <summary>
///     IAuditableEntity interface contains properties and methods for maintaining audit information for when
///     an entity was created and when it was last modified.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }

    [UsedImplicitly]
    DateTimeOffset? ModifiedAt { get; }

    void UpdateModifiedAt(DateTimeOffset? modifiedAt);
}