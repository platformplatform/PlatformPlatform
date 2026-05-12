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
    BackOfficeInvoiceRowKind RowKind,
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
    DateTimeOffset? CreditNotedAt,
    DateTimeOffset? RefundedAt
);

/// <summary>
///     Each PaymentTransaction projects to one Invoice row (always) plus an optional reversal row —
///     either CreditNote (when Stripe issued a credit note) or Refund (the edge case where a Stripe
///     pro-rated refund happened without a credit note). The Invoice row always carries the original
///     payment outcome (Paid / Pending / Failed); the reversal row carries the later state change.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackOfficeInvoiceRowKind
{
    Invoice,
    CreditNote,
    Refund
}

/// <summary>
///     Filter values exposed by the back-office invoices toolbar. <see cref="Paid" />, <see cref="Failed" />,
///     and <see cref="Pending" /> match Invoice rows by their original payment outcome.
///     <see cref="Refunded" /> matches RowKind=Refund rows (refund-without-credit-note edge case).
///     <see cref="HasCreditNote" /> matches RowKind=CreditNote rows. The "Refunds and credit notes" UI
///     toggle sends both <see cref="Refunded" /> and <see cref="HasCreditNote" />.
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
            .SelectMany(subscription => subscription.PaymentTransactions.SelectMany(transaction => ProjectRows(transaction, tenantsById[subscription.TenantId])))
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

    private static IEnumerable<BackOfficeInvoiceSummary> ProjectRows(PaymentTransaction transaction, Tenant tenant)
    {
        // Invoice row: always emitted. Status reflects the ORIGINAL payment outcome — a later refund
        // or credit note doesn't flip the invoice row to "Refunded"; it gets its own row instead. The
        // Refunded enum value at the transaction level becomes Succeeded on the invoice row because
        // every refunded transaction had a successful original charge.
        var invoiceRowStatus = transaction.Status == PaymentTransactionStatus.Refunded
            ? PaymentTransactionStatus.Succeeded
            : transaction.Status;

        yield return new BackOfficeInvoiceSummary(
            transaction.Id,
            BackOfficeInvoiceRowKind.Invoice,
            tenant.Id,
            tenant.Name,
            tenant.Logo.Url,
            transaction.Date,
            transaction.Plan,
            transaction.Amount,
            transaction.AmountExcludingTax,
            transaction.TaxAmount,
            transaction.Currency,
            invoiceRowStatus,
            transaction.FailureReason,
            transaction.InvoiceUrl,
            transaction.CreditNoteUrl,
            transaction.CreditNotedAt,
            transaction.RefundedAt
        );

        if (transaction.CreditNoteUrl is not null)
        {
            // CreditNote row: emitted whenever a Stripe credit note exists. Date falls through
            // CreditNotedAt → RefundedAt → original Date so legacy rows whose timestamps were never
            // backfilled still surface as their own row at the only timestamp we have. The producer
            // populates the precise dates on fresh Reconcile passes.
            yield return new BackOfficeInvoiceSummary(
                transaction.Id,
                BackOfficeInvoiceRowKind.CreditNote,
                tenant.Id,
                tenant.Name,
                tenant.Logo.Url,
                transaction.CreditNotedAt ?? transaction.RefundedAt ?? transaction.Date,
                transaction.Plan,
                transaction.Amount,
                transaction.AmountExcludingTax,
                transaction.TaxAmount,
                transaction.Currency,
                PaymentTransactionStatus.Refunded,
                transaction.FailureReason,
                transaction.InvoiceUrl,
                transaction.CreditNoteUrl,
                transaction.CreditNotedAt,
                transaction.RefundedAt
            );
        }
        else if (transaction.Status == PaymentTransactionStatus.Refunded || transaction.RefundedAt is not null)
        {
            // Refund row (edge case): Stripe pro-rated refunds don't always create a credit note —
            // when one happens the refund is the standalone reversal. Skip when a CreditNote sibling
            // already exists (per the user model: the credit note encompasses the refund).
            yield return new BackOfficeInvoiceSummary(
                transaction.Id,
                BackOfficeInvoiceRowKind.Refund,
                tenant.Id,
                tenant.Name,
                tenant.Logo.Url,
                transaction.RefundedAt ?? transaction.Date,
                transaction.Plan,
                transaction.Amount,
                transaction.AmountExcludingTax,
                transaction.TaxAmount,
                transaction.Currency,
                PaymentTransactionStatus.Refunded,
                transaction.FailureReason,
                transaction.InvoiceUrl,
                transaction.CreditNoteUrl,
                transaction.CreditNotedAt,
                transaction.RefundedAt
            );
        }
    }

    private static bool MatchesStatusFilter(BackOfficeInvoiceSummary summary, BackOfficeInvoiceStatusFilter filter)
    {
        return filter switch
        {
            // Invoice-side status filters only match RowKind=Invoice — reversal rows surface via Refunded / HasCreditNote.
            BackOfficeInvoiceStatusFilter.Paid => summary is { RowKind: BackOfficeInvoiceRowKind.Invoice, Status: PaymentTransactionStatus.Succeeded },
            BackOfficeInvoiceStatusFilter.Failed => summary is { RowKind: BackOfficeInvoiceRowKind.Invoice, Status: PaymentTransactionStatus.Failed },
            BackOfficeInvoiceStatusFilter.Pending => summary is { RowKind: BackOfficeInvoiceRowKind.Invoice, Status: PaymentTransactionStatus.Pending },
            BackOfficeInvoiceStatusFilter.Refunded => summary.RowKind == BackOfficeInvoiceRowKind.Refund,
            BackOfficeInvoiceStatusFilter.HasCreditNote => summary.RowKind == BackOfficeInvoiceRowKind.CreditNote,
            _ => false
        };
    }
}
