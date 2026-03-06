using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Domain;

namespace Account.Features.Users.Shared;

[PublicAPI]
public sealed record UserResponse(
    UserId Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    DateTimeOffset? DeletedAt,
    DateTimeOffset? LastSeenAt,
    string Email,
    string? FirstName,
    string? LastName,
    string? Title,
    UserRole Role,
    bool EmailConfirmed,
    string Locale,
    Avatar Avatar
)
{
    public static UserResponse FromUser(User user)
    {
        return new UserResponse(
            user.Id, user.CreatedAt, user.ModifiedAt, user.DeletedAt,
            user.LastSeenAt, user.Email, user.FirstName, user.LastName,
            user.Title, user.Role, user.EmailConfirmed, user.Locale,
            user.Avatar
        );
    }
}
