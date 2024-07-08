using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public sealed class User : AggregateRoot<UserId>
{
    private User(TenantId tenantId, string email, UserRole role, bool emailConfirmed)
        : base(UserId.NewId())
    {
        TenantId = tenantId;
        Email = email;
        Role = role;
        EmailConfirmed = emailConfirmed;
    }

    public TenantId TenantId { get; }

    public string Email { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Title { get; private set; }

    public UserRole Role { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public Avatar Avatar { get; private set; } = default!;

    public static User Create(TenantId tenantId, string email, UserRole role, bool emailConfirmed, string? gravatarUrl)
    {
        var avatar = new Avatar(gravatarUrl, IsGravatar: gravatarUrl is not null);
        return new User(tenantId, email, role, emailConfirmed) { Avatar = avatar };
    }

    public void Update(string firstName, string lastName, string title)
    {
        FirstName = firstName;
        LastName = lastName;
        Title = title;
    }

    public void UpdateEmail(string email)
    {
        Email = email;
    }

    public void ChangeUserRole(UserRole userRole)
    {
        Role = userRole;
    }

    public void UpdateAvatar(string avatarUrl)
    {
        Avatar = new Avatar(avatarUrl, Avatar.Version + 1);
    }

    public void RemoveAvatar()
    {
        Avatar = new Avatar(Version: Avatar.Version);
    }
}

public sealed record Avatar(string? Url = null, int Version = 0, bool IsGravatar = false);
