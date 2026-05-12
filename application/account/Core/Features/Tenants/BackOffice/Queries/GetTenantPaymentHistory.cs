using Account.Features.BackOffice.Invoices.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Tenants.BackOffice.Queries;

[PublicAPI]
public sealed record GetTenantPaymentHistoryQuery(int PageOffset = 0, int PageSize = 25) : IRequest<Result<TenantPaymentHistoryResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TenantId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record TenantPaymentHistoryResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, TenantPaymentTransaction[] Transactions);

[PublicAPI]
public sealed record TenantPaymentTransaction(
    PaymentTransactionId Id,
    BackOfficeInvoiceRowKind RowKind,
    decimal Amount,
    decimal AmountExcludingTax,
    decimal TaxAmount,
    string Currency,
    PaymentTransactionStatus Status,
    DateTimeOffset Date,
    DateTimeOffset? RefundedAt,
    string? FailureReason,
    string? InvoiceUrl,
    string? CreditNoteUrl,
    DateTimeOffset? CreditNotedAt,
    SubscriptionPlan? Plan
);

public sealed class GetTenantPaymentHistoryQueryValidator : AbstractValidator<GetTenantPaymentHistoryQuery>
{
    public GetTenantPaymentHistoryQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetTenantPaymentHistoryHandler(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetTenantPaymentHistoryQuery, Result<TenantPaymentHistoryResponse>>
{
    public async Task<Result<TenantPaymentHistoryResponse>> Handle(GetTenantPaymentHistoryQuery query, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (tenant is null)
        {
            return Result<TenantPaymentHistoryResponse>.NotFound($"Tenant with id '{query.Id}' was not found.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(tenant.Id, cancellationToken);
        var rows = (subscription?.PaymentTransactions ?? [])
            .SelectMany(ProjectRows)
            .OrderByDescending(r => r.Date)
            .ToArray();

        var totalCount = rows.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<TenantPaymentHistoryResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = rows.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        return new TenantPaymentHistoryResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }

    private static IEnumerable<TenantPaymentTransaction> ProjectRows(PaymentTransaction transaction)
    {
        // Invoice row: always emitted. Status reflects the ORIGINAL payment outcome — a later refund
        // or credit note doesn't flip the invoice row to "Refunded"; it gets its own row instead.
        var invoiceRowStatus = transaction.Status == PaymentTransactionStatus.Refunded
            ? PaymentTransactionStatus.Succeeded
            : transaction.Status;

        yield return new TenantPaymentTransaction(
            transaction.Id, BackOfficeInvoiceRowKind.Invoice, transaction.Amount, transaction.AmountExcludingTax,
            transaction.TaxAmount, transaction.Currency, invoiceRowStatus, transaction.Date, transaction.RefundedAt,
            transaction.FailureReason, transaction.InvoiceUrl, transaction.CreditNoteUrl, transaction.CreditNotedAt, transaction.Plan
        );

        if (transaction.CreditNoteUrl is not null)
        {
            // CreditNote row: emitted whenever a Stripe credit note exists. Date falls through
            // CreditNotedAt → RefundedAt → original Date so legacy rows whose timestamps were never
            // backfilled still surface as their own reversal row.
            yield return new TenantPaymentTransaction(
                transaction.Id, BackOfficeInvoiceRowKind.CreditNote, transaction.Amount, transaction.AmountExcludingTax,
                transaction.TaxAmount, transaction.Currency, PaymentTransactionStatus.Refunded,
                transaction.CreditNotedAt ?? transaction.RefundedAt ?? transaction.Date, transaction.RefundedAt,
                transaction.FailureReason, transaction.InvoiceUrl, transaction.CreditNoteUrl, transaction.CreditNotedAt, transaction.Plan
            );
        }
        else if (transaction.Status == PaymentTransactionStatus.Refunded || transaction.RefundedAt is not null)
        {
            // Refund row (edge case): Stripe pro-rated refunds don't always create a credit note —
            // when one happens the refund is the standalone reversal. Skip when a CreditNote sibling
            // already exists (the credit note encompasses the refund).
            yield return new TenantPaymentTransaction(
                transaction.Id, BackOfficeInvoiceRowKind.Refund, transaction.Amount, transaction.AmountExcludingTax,
                transaction.TaxAmount, transaction.Currency, PaymentTransactionStatus.Refunded,
                transaction.RefundedAt ?? transaction.Date, transaction.RefundedAt,
                transaction.FailureReason, transaction.InvoiceUrl, transaction.CreditNoteUrl, transaction.CreditNotedAt, transaction.Plan
            );
        }
    }
}
