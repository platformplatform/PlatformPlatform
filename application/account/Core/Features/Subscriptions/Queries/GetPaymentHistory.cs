using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Queries;

[PublicAPI]
public sealed record GetPaymentHistoryQuery(int PageOffset = 0, int PageSize = 10) : IRequest<Result<PaymentHistoryResponse>>;

[PublicAPI]
public sealed record PaymentHistoryResponse(int TotalCount, PaymentTransactionResponse[] Transactions);

[PublicAPI]
public sealed record PaymentTransactionResponse(
    PaymentTransactionId Id,
    decimal Amount,
    string Currency,
    PaymentTransactionStatus Status,
    DateTimeOffset Date,
    string? InvoiceUrl,
    string? CreditNoteUrl
);

public sealed class GetPaymentHistoryHandler(ISubscriptionRepository subscriptionRepository, IExecutionContext executionContext)
    : IRequestHandler<GetPaymentHistoryQuery, Result<PaymentHistoryResponse>>
{
    public async Task<Result<PaymentHistoryResponse>> Handle(GetPaymentHistoryQuery query, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        var allTransactions = subscription.PaymentTransactions
            .OrderByDescending(t => t.Date)
            .ToArray();

        var paginatedTransactions = allTransactions
            .Skip(query.PageOffset * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new PaymentTransactionResponse(t.Id, t.Amount, t.Currency, t.Status, t.Date, t.InvoiceUrl, t.CreditNoteUrl))
            .ToArray();

        return new PaymentHistoryResponse(allTransactions.Length, paginatedTransactions);
    }
}
