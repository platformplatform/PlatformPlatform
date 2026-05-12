using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRecentSignupsQuery(int Limit = 6)
    : IRequest<Result<BackOfficeDashboardRecentSignupsResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRecentSignupsResponse(BackOfficeDashboardRecentSignup[] Signups);

[PublicAPI]
public sealed record BackOfficeDashboardRecentSignup(
    TenantId TenantId,
    string Name,
    string? TenantLogoUrl,
    DateTimeOffset CreatedAt,
    BackOfficeDashboardRecentSignupOwner? Owner
);

[PublicAPI]
public sealed record BackOfficeDashboardRecentSignupOwner(UserId UserId, string? FirstName, string? LastName, string Email);

public sealed class GetDashboardRecentSignupsQueryValidator : AbstractValidator<GetDashboardRecentSignupsQuery>
{
    public GetDashboardRecentSignupsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

public sealed class GetDashboardRecentSignupsHandler(ITenantRepository tenantRepository, IUserRepository userRepository)
    : IRequestHandler<GetDashboardRecentSignupsQuery, Result<BackOfficeDashboardRecentSignupsResponse>>
{
    public async Task<Result<BackOfficeDashboardRecentSignupsResponse>> Handle(GetDashboardRecentSignupsQuery query, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetMostRecentSignupsUnfilteredAsync(query.Limit, cancellationToken);
        var tenantIds = tenants.Select(t => t.Id).ToArray();
        var ownerByTenantId = await userRepository.GetFirstOwnerByTenantIdsUnfilteredAsync(tenantIds, cancellationToken);

        var signups = tenants.Select(tenant =>
            {
                var owner = ownerByTenantId.GetValueOrDefault(tenant.Id);
                return new BackOfficeDashboardRecentSignup(
                    tenant.Id,
                    tenant.Name,
                    tenant.Logo.Url,
                    tenant.CreatedAt,
                    owner is null ? null : new BackOfficeDashboardRecentSignupOwner(owner.Id, owner.FirstName, owner.LastName, owner.Email)
                );
            }
        ).ToArray();

        return new BackOfficeDashboardRecentSignupsResponse(signups);
    }
}
