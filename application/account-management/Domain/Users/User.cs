using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User(TenantId tenantId, string email, UserRole userRole, bool emailConfirmed)
        : base(UserId.NewId())
    {
        TenantId = tenantId;
        Email = email;
        UserRole = userRole;
        EmailConfirmed = emailConfirmed;
    }

    public TenantId TenantId { get; }

    public string Email { get; private set; }

    [UsedImplicitly]
    public string? FirstName { get; private set; }

    [UsedImplicitly]
    public string? LastName { get; private set; }

    public UserRole UserRole { get; private set; }

    [UsedImplicitly]
    public bool EmailConfirmed { get; private set; }

    public static User Create(TenantId tenantId, string email, UserRole userRole, bool emailConfirmed)
    {
        return new User(tenantId, email, userRole, emailConfirmed);
    }

    public void Update(string email, UserRole userRole)
    {
        Email = email;
        UserRole = userRole;
    }
}