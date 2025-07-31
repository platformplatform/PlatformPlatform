using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Queries;

public sealed record GetTenantsForUserQuery : IRequest<Result<GetTenantsForUserResponse>>;

public sealed record GetTenantsForUserResponse(TenantInfo[] Tenants);

public sealed record TenantInfo(TenantId TenantId, string? TenantName, UserId UserId, string? LogoUrl, bool IsNew);

internal sealed class GetTenantsForUserQueryHandler(
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IExecutionContext executionContext
) : IRequestHandler<GetTenantsForUserQuery, Result<GetTenantsForUserResponse>>
{
    public async Task<Result<GetTenantsForUserResponse>> Handle(GetTenantsForUserQuery request, CancellationToken cancellationToken)
    {
        // Find all users with the same email across all tenants
        var users = await userRepository.GetUsersByEmailUnfilteredAsync(executionContext.UserInfo.Email!, cancellationToken);
        if (users.Length == 0)
        {
            return Result<GetTenantsForUserResponse>.BadRequest("No tenants found for user.");
        }

        var tenantIds = users.Select(u => u.TenantId).ToArray();
        var tenants = await tenantRepository.GetByIdsAsync(tenantIds, cancellationToken);

        var tenantInfoList = tenants.Select(t =>
            {
                var user = users.Single(u => u.TenantId == t.Id);
                return new TenantInfo(t.Id, t.Name, user.Id, t.Logo.Url, !user.EmailConfirmed);
            }
        ).ToArray();

        return new GetTenantsForUserResponse(tenantInfoList);
    }
}
