using Account.Features.Subscriptions.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardMrrTrendQuery(DashboardTrendPeriod Period = DashboardTrendPeriod.Last30Days)
    : IRequest<Result<BackOfficeDashboardMrrTrendResponse>>;

[PublicAPI]
public sealed record BackOfficeDashboardMrrTrendResponse(
    DashboardTrendPeriod Period,
    string Currency,
    BackOfficeDashboardMrrTrendPoint[] Points
);

[PublicAPI]
public sealed record BackOfficeDashboardMrrTrendPoint(DateOnly Date, decimal MonthlyRecurringRevenue);

public sealed class GetDashboardMrrTrendQueryValidator : AbstractValidator<GetDashboardMrrTrendQuery>
{
    public GetDashboardMrrTrendQueryValidator()
    {
        RuleFor(x => x.Period).Must(p => Enum.IsDefined(typeof(DashboardTrendPeriod), p)).WithMessage("Period must be one of Last7Days, Last30Days, or Last90Days.");
    }
}

public sealed class GetDashboardMrrTrendHandler(ISubscriptionRepository subscriptionRepository, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardMrrTrendQuery, Result<BackOfficeDashboardMrrTrendResponse>>
{
    private const string DefaultCurrency = "DKK";

    public async Task<Result<BackOfficeDashboardMrrTrendResponse>> Handle(GetDashboardMrrTrendQuery query, CancellationToken cancellationToken)
    {
        var days = DashboardTrendPeriods.GetDays(query.Period);
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var startDate = today.AddDays(-(days - 1));

        var subscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);

        var points = new BackOfficeDashboardMrrTrendPoint[days];
        for (var index = 0; index < days; index++)
        {
            var date = startDate.AddDays(index);
            var endOfDay = new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

            // A subscription contributes to MRR on a day if it was already subscribed (or backdated) at end-of-day,
            // and has a known price. Cancellations are not stored as a separate timestamp so the historical signal
            // is approximated from SubscribedSince forward; CurrentPriceAmount is treated as steady over the period.
            var dailyMrr = subscriptions
                .Where(s => s is { CurrentPriceAmount: not null, SubscribedSince: { } subscribedSince } && subscribedSince < endOfDay)
                .Sum(s => s.CurrentPriceAmount!.Value);

            points[index] = new BackOfficeDashboardMrrTrendPoint(date, dailyMrr);
        }

        return new BackOfficeDashboardMrrTrendResponse(query.Period, DefaultCurrency, points);
    }
}
