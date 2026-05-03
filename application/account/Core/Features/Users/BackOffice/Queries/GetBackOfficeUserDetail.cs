using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Users.BackOffice.Queries;

[PublicAPI]
public sealed record GetBackOfficeUserDetailQuery(UserId Id) : IRequest<Result<BackOfficeUserDetailResponse>>;

[PublicAPI]
public sealed record BackOfficeUserDetailResponse(
    UserId Id,
    TenantId TenantId,
    string TenantName,
    string Email,
    string? FirstName,
    string? LastName,
    string? Title,
    UserRole Role,
    bool EmailConfirmed,
    string Locale,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    DateTimeOffset? LastSeenAt,
    string? AvatarUrl,
    BackOfficeUserTenantMembership[] TenantMemberships
);

// A "tenant membership" is another user record sharing the same email in a different tenant. Each row in the back-office
// User detail Tenants section corresponds to a single user-record-per-tenant; we expose its UserId so the frontend can
// link the row to that other user's detail page when needed.
[PublicAPI]
public sealed record BackOfficeUserTenantMembership(
    UserId UserId,
    TenantId TenantId,
    string TenantName,
    UserRole Role,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt
);

public sealed class GetBackOfficeUserDetailHandler(IUserRepository userRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetBackOfficeUserDetailQuery, Result<BackOfficeUserDetailResponse>>
{
    public async Task<Result<BackOfficeUserDetailResponse>> Handle(GetBackOfficeUserDetailQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (user is null)
        {
            return Result<BackOfficeUserDetailResponse>.NotFound($"User with id '{query.Id}' was not found.");
        }

        // The "Tenants" section on the User detail page lists every tenant this person belongs to. Each tenant has its
        // own user record (same email, different TenantId), so we look them up unfiltered by email.
        var membershipUsers = await userRepository.GetUsersByEmailUnfilteredAsync(user.Email, cancellationToken);
        var tenantIds = membershipUsers.Select(u => u.TenantId).Append(user.TenantId).Distinct().ToArray();
        var tenantNames = await tenantRepository.GetNamesByIdsUnfilteredAsync(tenantIds, cancellationToken);

        var memberships = membershipUsers.Select(u => new BackOfficeUserTenantMembership(
                u.Id,
                u.TenantId,
                tenantNames.GetValueOrDefault(u.TenantId, string.Empty),
                u.Role,
                u.EmailConfirmed,
                u.CreatedAt,
                u.LastSeenAt
            )
        ).ToArray();

        return new BackOfficeUserDetailResponse(
            user.Id,
            user.TenantId,
            tenantNames.GetValueOrDefault(user.TenantId, string.Empty),
            user.Email,
            user.FirstName,
            user.LastName,
            user.Title,
            user.Role,
            user.EmailConfirmed,
            user.Locale,
            user.CreatedAt,
            user.ModifiedAt,
            user.LastSeenAt,
            user.Avatar.Url,
            memberships
        );
    }
}
