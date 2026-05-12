using Account.Features.Subscriptions.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.BillingDrift.Queries;

[PublicAPI]
public sealed record GetBillingDriftSummaryQuery : IRequest<Result<BillingDriftSummaryResponse>>;

[PublicAPI]
public sealed record BillingDriftSummaryResponse(int SubscriptionsWithDriftCount);

public sealed class GetBillingDriftSummaryHandler(ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetBillingDriftSummaryQuery, Result<BillingDriftSummaryResponse>>
{
    public async Task<Result<BillingDriftSummaryResponse>> Handle(GetBillingDriftSummaryQuery query, CancellationToken cancellationToken)
    {
        var count = await subscriptionRepository.CountWithDriftDetectedUnfilteredAsync(cancellationToken);
        return new BillingDriftSummaryResponse(count);
    }
}
