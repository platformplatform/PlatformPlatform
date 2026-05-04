using Account.Features.Authentication.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Users.BackOffice.Queries;

[PublicAPI]
public sealed record GetBackOfficeUserSessionsQuery(int PageOffset = 0, int PageSize = 25) : IRequest<Result<BackOfficeUserSessionsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public UserId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record BackOfficeUserSessionsResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, BackOfficeUserSession[] Sessions);

[PublicAPI]
public sealed record BackOfficeUserSession(
    SessionId Id,
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    LoginMethod LoginMethod,
    DeviceType DeviceType,
    string UserAgent,
    string IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset? RevokedAt,
    SessionRevokedReason? RevokedReason,
    DateTimeOffset ExpiresAt
);

public sealed class GetBackOfficeUserSessionsQueryValidator : AbstractValidator<GetBackOfficeUserSessionsQuery>
{
    public GetBackOfficeUserSessionsQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetBackOfficeUserSessionsHandler(IUserRepository userRepository, ISessionRepository sessionRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetBackOfficeUserSessionsQuery, Result<BackOfficeUserSessionsResponse>>
{
    public async Task<Result<BackOfficeUserSessionsResponse>> Handle(GetBackOfficeUserSessionsQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (user is null)
        {
            return Result<BackOfficeUserSessionsResponse>.NotFound($"User with id '{query.Id}' was not found.");
        }

        // The Sessions list aggregates activity across every user record sharing this email (one record per tenant),
        // so we look up all sibling user ids and ask for their sessions together. The lookup always includes the
        // queried user record itself, so its sessions are naturally part of the result.
        var membershipUsers = await userRepository.GetUsersByEmailUnfilteredAsync(user.Email, cancellationToken);
        var userIds = membershipUsers.Select(u => u.Id).ToArray();

        var (sessions, totalCount, totalPages) = await sessionRepository.GetSessionsForUsersUnfilteredAsync(
            userIds,
            query.PageOffset,
            query.PageSize,
            cancellationToken
        );

        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<BackOfficeUserSessionsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var tenantIds = sessions.Select(s => s.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var summaries = sessions.Select(s =>
            {
                var tenant = tenantsById.GetValueOrDefault(s.TenantId);
                return new BackOfficeUserSession(
                    s.Id,
                    s.TenantId,
                    tenant?.Name ?? string.Empty,
                    tenant?.Logo.Url,
                    s.LoginMethod,
                    s.DeviceType,
                    s.UserAgent,
                    s.IpAddress,
                    s.CreatedAt,
                    s.ModifiedAt,
                    s.RevokedAt,
                    s.RevokedReason,
                    s.ExpiresAt
                );
            }
        ).ToArray();

        return new BackOfficeUserSessionsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, summaries);
    }
}
