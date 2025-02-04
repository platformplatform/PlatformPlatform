using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Users.Queries;

[PublicAPI]
public sealed record GetUserSummaryQuery : IRequest<Result<UserSummaryResponse>>;

[PublicAPI]
public sealed record UserSummaryResponse(int TotalUsers, int ActiveUsers, int PendingUsers);

public sealed class GetUserSummaryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserSummaryQuery, Result<UserSummaryResponse>>
{
    public async Task<Result<UserSummaryResponse>> Handle(GetUserSummaryQuery query, CancellationToken cancellationToken)
    {
        var (totalUsers, activeUsers, pendingUsers) = await userRepository.GetUserSummaryAsync(cancellationToken);
        return new UserSummaryResponse(totalUsers, activeUsers, pendingUsers);
    }
}
