using Account.Features.Authentication.Domain;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.ExternalAuthentication.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRecentLoginsQuery(int Limit = 6)
    : IRequest<Result<BackOfficeDashboardRecentLoginsResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRecentLoginsResponse(BackOfficeDashboardLogin[] Logins);

[PublicAPI]
public sealed record BackOfficeDashboardLogin(
    UserId? UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    TenantId? TenantId,
    string? TenantName,
    string? TenantLogoUrl,
    LoginMethod Method,
    DateTimeOffset OccurredAt
);

public sealed class GetDashboardRecentLoginsQueryValidator : AbstractValidator<GetDashboardRecentLoginsQuery>
{
    public GetDashboardRecentLoginsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

public sealed class GetDashboardRecentLoginsHandler(
    IEmailLoginRepository emailLoginRepository,
    IExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    TimeProvider timeProvider
) : IRequestHandler<GetDashboardRecentLoginsQuery, Result<BackOfficeDashboardRecentLoginsResponse>>
{
    private const int LookbackDays = 30;

    public async Task<Result<BackOfficeDashboardRecentLoginsResponse>> Handle(GetDashboardRecentLoginsQuery query, CancellationToken cancellationToken)
    {
        var since = timeProvider.GetUtcNow().AddDays(-LookbackDays);
        var emailLogins = await emailLoginRepository.GetCompletedSinceAsync(since, cancellationToken);
        var externalLogins = await externalLoginRepository.GetSucceededSinceAsync(since, cancellationToken);

        var entries = emailLogins.Select(e => new LoginEntry(e.Email, LoginMethod.OneTimePassword, e.CreatedAt))
            .Concat(externalLogins.Where(e => e.Email is not null).Select(e => new LoginEntry(e.Email!, MapExternalMethod(e.ProviderType), e.CreatedAt)))
            .OrderByDescending(e => e.OccurredAt)
            .Take(query.Limit)
            .ToArray();

        if (entries.Length == 0) return new BackOfficeDashboardRecentLoginsResponse([]);

        // Login aggregates store email rather than user id (an email can map to multiple users across tenants).
        // Resolve to the first user per email so the dashboard row can show a name and the tenant context;
        // operators can still drill into the user/account detail pages for full disambiguation.
        var distinctEmails = entries.Select(e => e.Email).Distinct().ToArray();
        var userByEmail = new Dictionary<string, User>();
        foreach (var email in distinctEmails)
        {
            var users = await userRepository.GetUsersByEmailUnfilteredAsync(email, cancellationToken);
            if (users.Length > 0) userByEmail[email] = users[0];
        }

        var tenantIds = userByEmail.Values.Select(u => u.TenantId).Distinct().ToArray();
        var tenants = tenantIds.Length == 0
            ? []
            : await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var logins = entries.Select(entry =>
            {
                var user = userByEmail.GetValueOrDefault(entry.Email);
                var tenant = user is not null ? tenantsById.GetValueOrDefault(user.TenantId) : null;
                return new BackOfficeDashboardLogin(
                    user?.Id,
                    entry.Email,
                    user?.FirstName,
                    user?.LastName,
                    user?.Avatar.Url,
                    user?.TenantId,
                    tenant?.Name,
                    tenant?.Logo.Url,
                    entry.Method,
                    entry.OccurredAt
                );
            }
        ).ToArray();

        return new BackOfficeDashboardRecentLoginsResponse(logins);
    }

    private static LoginMethod MapExternalMethod(ExternalProviderType providerType)
    {
        return providerType switch
        {
            ExternalProviderType.Google => LoginMethod.Google,
            _ => throw new UnreachableException($"Unknown external provider type '{providerType}'.")
        };
    }

    private sealed record LoginEntry(string Email, LoginMethod Method, DateTimeOffset OccurredAt);
}
