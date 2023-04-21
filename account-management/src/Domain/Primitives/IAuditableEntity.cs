namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; }

    DateTime? ModifiedAt { get; }

    void UpdateModifiedAt(DateTime? modifiedAt);
}