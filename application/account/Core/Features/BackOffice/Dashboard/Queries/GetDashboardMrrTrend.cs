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
    BackOfficeDashboardMrrTrendPoint[] Points,
    BackOfficeDashboardMrrTrendPoint[] PriorPoints
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
        var priorStartDate = startDate.AddDays(-days);

        var subscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);

        var points = new BackOfficeDashboardMrrTrendPoint[days];
        var priorPoints = new BackOfficeDashboardMrrTrendPoint[days];
        for (var index = 0; index < days; index++)
        {
            var currentDate = startDate.AddDays(index);
            var priorDate = priorStartDate.AddDays(index);
            points[index] = new BackOfficeDashboardMrrTrendPoint(currentDate, ComputeDailyMrr(subscriptions, currentDate));
            priorPoints[index] = new BackOfficeDashboardMrrTrendPoint(priorDate, ComputeDailyMrr(subscriptions, priorDate));
        }

        return new BackOfficeDashboardMrrTrendResponse(query.Period, DefaultCurrency, points, priorPoints);
    }

    // A subscription contributes to MRR on a day if it was already subscribed (or backdated) at end-of-day, and has a
    // known price. Cancellations are not stored as a separate timestamp, so the historical signal is approximated from
    // SubscribedSince forward; the per-subscription contribution is forward MRR (0 when cancelling at period end,
    // ScheduledPriceAmount when a downgrade is queued, otherwise CurrentPriceAmount), matching the KPI tile and the
    // per-account MrrAmount tile. The scheduled state is treated as steady over the period.
    private static decimal ComputeDailyMrr(Subscription[] subscriptions, DateOnly date)
    {
        var endOfDay = new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return subscriptions
            .Where(s => s is { CurrentPriceAmount: not null, SubscribedSince: { } subscribedSince } && subscribedSince < endOfDay)
            .Sum(s => s.CancelAtPeriodEnd
                ? 0m
                : s.ScheduledPriceAmount ?? s.CurrentPriceAmount!.Value
            );
    }
}
