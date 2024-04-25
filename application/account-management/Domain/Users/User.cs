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
    
    public Avatar Avatar { get; private set; } = default!;
    
    public static User Create(TenantId tenantId, string email, UserRole userRole, bool emailConfirmed, string? gravatarUrl)
    {
        var avatar = new Avatar(gravatarUrl, IsGravatar: gravatarUrl is not null);
        return new User(tenantId, email, userRole, emailConfirmed) { Avatar = avatar };
    }
    
    public void Update(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
    
    public void UpdateEmail(string email)
    {
        Email = email;
    }
    
    public void ChangeUserRole(UserRole userRole)
    {
        UserRole = userRole;
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
