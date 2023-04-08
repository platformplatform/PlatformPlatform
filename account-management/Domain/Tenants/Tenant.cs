using System.ComponentModel.DataAnnotations;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : Entity, IAggregateRoot
{
    [MinLength(1)] [MaxLength(50)] public required string Name { get; set; }
}