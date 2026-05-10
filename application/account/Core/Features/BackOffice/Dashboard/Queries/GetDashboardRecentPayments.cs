using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardRecentPaymentsQuery(int Limit = 6)
    : IRequest<Result<BackOfficeDashboardRecentPaymentsResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardRecentPaymentsResponse(BackOfficeDashboardPayment[] Payments);

[PublicAPI]
public sealed record BackOfficeDashboardPayment(
    PaymentTransactionId Id,
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    DateTimeOffset Date,
    SubscriptionPlan? Plan,
    decimal Amount,
    string Currency,
    PaymentTransactionStatus Status
);

public sealed class GetDashboardRecentPaymentsQueryValidator : AbstractValidator<GetDashboardRecentPaymentsQuery>
{
    public GetDashboardRecentPaymentsQueryValidator()
    {
        RuleFor(x => x.Limit).InclusiveBetween(1, 50).WithMessage("Limit must be between 1 and 50.");
    }
}

public sealed class GetDashboardRecentPaymentsHandler(ISubscriptionRepository subscriptionRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetDashboardRecentPaymentsQuery, Result<BackOfficeDashboardRecentPaymentsResponse>>
{
    public async Task<Result<BackOfficeDashboardRecentPaymentsResponse>> Handle(GetDashboardRecentPaymentsQuery query, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository.GetAllWithTransactionsUnfilteredAsync(cancellationToken);
        if (subscriptions.Length == 0) return new BackOfficeDashboardRecentPaymentsResponse([]);

        var tenantIds = subscriptions.Select(s => s.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var payments = subscriptions
            .Where(s => tenantsById.ContainsKey(s.TenantId))
            .SelectMany(subscription => subscription.PaymentTransactions.Select(transaction =>
                    {
                        var tenant = tenantsById[subscription.TenantId];
                        return new BackOfficeDashboardPayment(
                            transaction.Id,
                            tenant.Id,
                            tenant.Name,
                            tenant.Logo.Url,
                            transaction.Date,
                            transaction.Plan,
                            transaction.Amount,
                            transaction.Currency,
                            transaction.Status
                        );
                    }
                )
            )
            .OrderByDescending(p => p.Date)
            .Take(query.Limit)
            .ToArray();

        return new BackOfficeDashboardRecentPaymentsResponse(payments);
    }
}
