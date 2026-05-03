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
    decimal Amount,
    string Currency,
    PaymentTransactionStatus Status,
    DateTimeOffset Date,
    string? FailureReason,
    string? InvoiceUrl,
    string? CreditNoteUrl
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
        var transactions = subscription?.PaymentTransactions.OrderByDescending(t => t.Date).ToArray() ?? [];

        var totalCount = transactions.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<TenantPaymentHistoryResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var paged = transactions
            .Skip(query.PageOffset * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TenantPaymentTransaction(t.Id, t.Amount, t.Currency, t.Status, t.Date, t.FailureReason, t.InvoiceUrl, t.CreditNoteUrl))
            .ToArray();

        return new TenantPaymentHistoryResponse(totalCount, query.PageSize, totalPages, query.PageOffset, paged);
    }
}
