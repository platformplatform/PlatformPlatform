namespace PlatformPlatform.SharedKernel.Domain;

/// <summary>
///     IAuditableEntity interface contains properties and methods for maintaining audit information for when
///     an entity was created and when it was last modified.
/// </summary>
internal interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset? ModifiedAt { get; }

    void UpdateModifiedAt(DateTimeOffset? modifiedAt);
}
