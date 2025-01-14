using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Users.Domain;

public sealed class User : AggregateRoot<UserId>, ITenantScopedEntity
{
    private string _email = string.Empty;

    private User(TenantId tenantId, string email, UserRole role, bool emailConfirmed, string? locale)
        : base(UserId.NewId())
    {
        Email = email;
        TenantId = tenantId;
        Role = role;
        EmailConfirmed = emailConfirmed;
        Locale = locale ?? string.Empty;
        Avatar = new Avatar();
    }

    public string Email
    {
        get => _email;
        private set => _email = value.ToLowerInvariant();
    }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Title { get; private set; }

    public UserRole Role { get; private set; }

    public bool EmailConfirmed { get; private set; }

    public Avatar Avatar { get; private set; }

    public string Locale { get; private set; }

    public TenantId TenantId { get; }

    public static User Create(TenantId tenantId, string email, UserRole role, bool emailConfirmed, string? locale)
    {
        return new User(tenantId, email, role, emailConfirmed, locale);
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

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    public void ChangeUserRole(UserRole userRole)
    {
        Role = userRole;
    }

    public void UpdateAvatar(string avatarUrl, bool isGravatar)
    {
        Avatar = new Avatar(avatarUrl, Avatar.Version + 1, isGravatar);
    }

    public void RemoveAvatar()
    {
        Avatar = new Avatar(Version: Avatar.Version);
    }

    public void ChangeLocale(string locale)
    {
        Locale = locale;
    }
}

public sealed record Avatar(string? Url = null, int Version = 0, bool IsGravatar = false);
