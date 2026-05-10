using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.BackOffice.Invoices.Queries;

[PublicAPI]
public sealed record GetBackOfficeInvoicesQuery(
    string? Search = null,
    BackOfficeInvoiceStatusFilter[]? Statuses = null,
    SortableBackOfficeInvoiceProperties OrderBy = SortableBackOfficeInvoiceProperties.Date,
    SortOrder SortOrder = SortOrder.Descending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<BackOfficeInvoicesResponse>>
{
    public string? Search { get; } = Search?.Trim().ToLower();

    public BackOfficeInvoiceStatusFilter[] Statuses { get; } = Statuses ?? [];
}

[PublicAPI]
public sealed record BackOfficeInvoicesResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, BackOfficeInvoiceSummary[] Invoices);

[PublicAPI]
public sealed record BackOfficeInvoiceSummary(
    PaymentTransactionId Id,
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    DateTimeOffset Date,
    SubscriptionPlan? Plan,
    decimal Amount,
    decimal AmountExcludingTax,
    decimal TaxAmount,
    string Currency,
    PaymentTransactionStatus Status,
    string? FailureReason,
    string? InvoiceUrl,
    string? CreditNoteUrl,
    DateTimeOffset? RefundedAt
);

/// <summary>
///     Filter values exposed by the back-office invoices toolbar. The first four mirror
///     <see cref="PaymentTransactionStatus" /> directly; <see cref="HasCreditNote" /> is a virtual filter
///     that selects rows where Stripe issued a credit note (CreditNoteUrl set) regardless of payment status.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackOfficeInvoiceStatusFilter
{
    Paid,
    Refunded,
    Failed,
    Pending,
    HasCreditNote
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableBackOfficeInvoiceProperties
{
    Date,
    TenantName,
    Total,
    Status
}

public sealed class GetBackOfficeInvoicesQueryValidator : AbstractValidator<GetBackOfficeInvoicesQuery>
{
    public GetBackOfficeInvoicesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Search must be no longer than 100 characters.");
        RuleFor(x => x.Statuses.Length).LessThanOrEqualTo(10).WithMessage("Status filter must contain no more than 10 values.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetBackOfficeInvoicesHandler(ISubscriptionRepository subscriptionRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetBackOfficeInvoicesQuery, Result<BackOfficeInvoicesResponse>>
{
    public async Task<Result<BackOfficeInvoicesResponse>> Handle(GetBackOfficeInvoicesQuery query, CancellationToken cancellationToken)
    {
        var subscriptions = await subscriptionRepository.GetAllWithTransactionsUnfilteredAsync(cancellationToken);
        if (subscriptions.Length == 0)
        {
            return new BackOfficeInvoicesResponse(0, query.PageSize, 0, query.PageOffset, []);
        }

        var tenantIds = subscriptions.Select(s => s.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var summaries = subscriptions
            .Where(s => tenantsById.ContainsKey(s.TenantId))
            .SelectMany(subscription => subscription.PaymentTransactions.Select(transaction =>
                    {
                        var tenant = tenantsById[subscription.TenantId];
                        return new BackOfficeInvoiceSummary(
                            transaction.Id,
                            tenant.Id,
                            tenant.Name,
                            tenant.Logo.Url,
                            transaction.Date,
                            transaction.Plan,
                            transaction.Amount,
                            transaction.AmountExcludingTax,
                            transaction.TaxAmount,
                            transaction.Currency,
                            transaction.Status,
                            transaction.FailureReason,
                            transaction.InvoiceUrl,
                            transaction.CreditNoteUrl,
                            transaction.RefundedAt
                        );
                    }
                )
            )
            .ToArray();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            summaries = [.. summaries.Where(s => s.TenantName.ToLower().Contains(query.Search))];
        }

        if (query.Statuses.Length > 0)
        {
            summaries = [.. summaries.Where(s => query.Statuses.Any(filter => MatchesStatusFilter(s, filter)))];
        }

        var ordered = (query.OrderBy, query.SortOrder) switch
        {
            (SortableBackOfficeInvoiceProperties.TenantName, SortOrder.Ascending) => summaries.OrderBy(s => s.TenantName).ThenByDescending(s => s.Date),
            (SortableBackOfficeInvoiceProperties.TenantName, _) => summaries.OrderByDescending(s => s.TenantName).ThenByDescending(s => s.Date),
            (SortableBackOfficeInvoiceProperties.Total, SortOrder.Ascending) => summaries.OrderBy(s => s.Amount).ThenByDescending(s => s.Date),
            (SortableBackOfficeInvoiceProperties.Total, _) => summaries.OrderByDescending(s => s.Amount).ThenByDescending(s => s.Date),
            (SortableBackOfficeInvoiceProperties.Status, SortOrder.Ascending) => summaries.OrderBy(s => s.Status).ThenByDescending(s => s.Date),
            (SortableBackOfficeInvoiceProperties.Status, _) => summaries.OrderByDescending(s => s.Status).ThenByDescending(s => s.Date),
            (SortableBackOfficeInvoiceProperties.Date, SortOrder.Ascending) => summaries.OrderBy(s => s.Date),
            _ => summaries.OrderByDescending(s => s.Date)
        };

        var totalCount = summaries.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<BackOfficeInvoicesResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = ordered.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new BackOfficeInvoicesResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }

    private static bool MatchesStatusFilter(BackOfficeInvoiceSummary summary, BackOfficeInvoiceStatusFilter filter)
    {
        return filter switch
        {
            BackOfficeInvoiceStatusFilter.Paid => summary.Status == PaymentTransactionStatus.Succeeded,
            BackOfficeInvoiceStatusFilter.Refunded => summary.Status == PaymentTransactionStatus.Refunded,
            BackOfficeInvoiceStatusFilter.Failed => summary.Status == PaymentTransactionStatus.Failed,
            BackOfficeInvoiceStatusFilter.Pending => summary.Status == PaymentTransactionStatus.Pending,
            BackOfficeInvoiceStatusFilter.HasCreditNote => !string.IsNullOrEmpty(summary.CreditNoteUrl),
            _ => false
        };
    }
}
