namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }

    DateTime? ModifiedAt { get; set; }
}