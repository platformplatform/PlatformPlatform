using Account.Features.Subscriptions.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.BillingDrift.Queries;

[PublicAPI]
public sealed record GetUnsyncedSubscriptionsSummaryQuery : IRequest<Result<UnsyncedSubscriptionsSummaryResponse>>;

[PublicAPI]
public sealed record UnsyncedSubscriptionsSummaryResponse(int UnsyncedSubscriptionsCount);

public sealed class GetUnsyncedSubscriptionsSummaryHandler(ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetUnsyncedSubscriptionsSummaryQuery, Result<UnsyncedSubscriptionsSummaryResponse>>
{
    public async Task<Result<UnsyncedSubscriptionsSummaryResponse>> Handle(GetUnsyncedSubscriptionsSummaryQuery query, CancellationToken cancellationToken)
    {
        var count = await subscriptionRepository.CountWithoutBillingEventsUnfilteredAsync(cancellationToken);
        return new UnsyncedSubscriptionsSummaryResponse(count);
    }
}
