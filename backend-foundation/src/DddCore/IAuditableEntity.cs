using JetBrains.Annotations;

namespace PlatformPlatform.Foundation.DddCore;

/// <summary>
///     IAuditableEntity interface contains properties and methods for maintaining audit information for when
///     an entity was created and when it was last modified.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }

    [UsedImplicitly]
    DateTime? ModifiedAt { get; }

    void UpdateModifiedAt(DateTime? modifiedAt);
}