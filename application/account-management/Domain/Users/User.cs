using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User(TenantId tenantId, string email, UserRole userRole) : base(UserId.NewId())
    {
        TenantId = tenantId;
        Email = email;
        UserRole = userRole;
    }

    public TenantId TenantId { get; }

    public string Email { get; private set; }

    public UserRole UserRole { get; private set; }

    public static User Create(TenantId tenantId, string email, UserRole userRole)
    {
        return new User(tenantId, email, userRole);
    }

    public void Update(string email, UserRole userRole)
    {
        Email = email;
        UserRole = userRole;
    }
}