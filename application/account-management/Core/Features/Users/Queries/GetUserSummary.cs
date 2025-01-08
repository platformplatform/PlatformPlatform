using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Users.Queries;

[PublicAPI]
public sealed record GetUserSummaryQuery : IRequest<Result<GetUserSummaryResponse>>;

[PublicAPI]
public sealed record GetUserSummaryResponse(int TotalUsers, int ActiveUsers, int PendingUsers);

public sealed class GetUserSummaryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserSummaryQuery, Result<GetUserSummaryResponse>>
{
    public async Task<Result<GetUserSummaryResponse>> Handle(GetUserSummaryQuery query, CancellationToken cancellationToken)
    {
        var (totalUsers, activeUsers, pendingUsers) = await userRepository.GetUserSummaryAsync(cancellationToken);
        return new GetUserSummaryResponse(totalUsers, activeUsers, pendingUsers);
    }
}
