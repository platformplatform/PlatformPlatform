namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : Entity, IAggregateRoot
{
    public Tenant(string name)
    {
        Name = name;
    }

    public required string Name { get; set; }
}