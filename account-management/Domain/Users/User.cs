using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    internal User(string email, UserRole userRole) : base(UserId.NewId())
    {
        Email = email;
        UserRole = userRole;
    }

    public string Email { get; private set; }

    public UserRole UserRole { get; private set; }

    public static User Create(string email, UserRole userRole)
    {
        return new User(email, userRole);
    }

    public void Update(string email, UserRole userRole)
    {
        Email = email;
        UserRole = userRole;
    }
}