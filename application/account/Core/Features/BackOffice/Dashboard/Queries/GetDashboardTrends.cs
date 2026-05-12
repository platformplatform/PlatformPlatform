using Account.Features.EmailAuthentication.Domain;
using Account.Features.ExternalAuthentication.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;

namespace Account.Features.BackOffice.Dashboard.Queries;

[PublicAPI]
public sealed record GetDashboardTrendsQuery(DashboardTrendMetric Metric, DashboardTrendPeriod Period)
    : IRequest<Result<BackOfficeDashboardTrendsResponse>>;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DashboardTrendMetric
{
    NewTenants,
    NewUsers,
    LoginActivity
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DashboardTrendPeriod
{
    Last7Days,
    Last30Days,
    Last90Days
}

[PublicAPI]
public sealed record BackOfficeDashboardTrendsResponse(
    DashboardTrendMetric Metric,
    DashboardTrendPeriod Period,
    BackOfficeDashboardTrendPoint[] Points,
    BackOfficeDashboardTrendPoint[] PriorPoints
);

[PublicAPI]
public sealed record BackOfficeDashboardTrendPoint(DateOnly Date, long Value);

public sealed class GetDashboardTrendsQueryValidator : AbstractValidator<GetDashboardTrendsQuery>
{
    public GetDashboardTrendsQueryValidator()
    {
        RuleFor(x => x.Metric).Must(m => Enum.IsDefined(typeof(DashboardTrendMetric), m)).WithMessage("Metric must be one of NewTenants, NewUsers, or LoginActivity.");
        RuleFor(x => x.Period).Must(p => Enum.IsDefined(typeof(DashboardTrendPeriod), p)).WithMessage("Period must be one of Last7Days, Last30Days, or Last90Days.");
    }
}

public sealed class GetDashboardTrendsHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    IEmailLoginRepository emailLoginRepository,
    IExternalLoginRepository externalLoginRepository,
    TimeProvider timeProvider
) : IRequestHandler<GetDashboardTrendsQuery, Result<BackOfficeDashboardTrendsResponse>>
{
    public async Task<Result<BackOfficeDashboardTrendsResponse>> Handle(GetDashboardTrendsQuery query, CancellationToken cancellationToken)
    {
        var days = DashboardTrendPeriods.GetDays(query.Period);
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var startDate = today.AddDays(-(days - 1));
        var priorStartDate = startDate.AddDays(-days);
        // Pull the prior window in the same query so the chart can render a comparison overlay without a second round-trip.
        var since = new DateTimeOffset(priorStartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var counts = query.Metric switch
        {
            DashboardTrendMetric.NewTenants => await CountNewTenantsPerDay(since, cancellationToken),
            DashboardTrendMetric.NewUsers => await CountNewUsersPerDay(since, cancellationToken),
            DashboardTrendMetric.LoginActivity => await CountLoginActivityPerDay(since, cancellationToken),
            _ => throw new UnreachableException($"Unsupported metric '{query.Metric}'.")
        };

        var points = new BackOfficeDashboardTrendPoint[days];
        var priorPoints = new BackOfficeDashboardTrendPoint[days];
        for (var index = 0; index < days; index++)
        {
            var currentDate = startDate.AddDays(index);
            var priorDate = priorStartDate.AddDays(index);
            points[index] = new BackOfficeDashboardTrendPoint(currentDate, counts.GetValueOrDefault(currentDate));
            priorPoints[index] = new BackOfficeDashboardTrendPoint(priorDate, counts.GetValueOrDefault(priorDate));
        }

        return new BackOfficeDashboardTrendsResponse(query.Metric, query.Period, points, priorPoints);
    }

    private async Task<Dictionary<DateOnly, long>> CountNewTenantsPerDay(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.GetCreatedSinceUnfilteredAsync(since, cancellationToken);
        return BucketByDay(tenants.Select(t => t.CreatedAt));
    }

    private async Task<Dictionary<DateOnly, long>> CountNewUsersPerDay(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetCreatedSinceUnfilteredAsync(since, cancellationToken);
        return BucketByDay(users.Select(u => u.CreatedAt));
    }

    private async Task<Dictionary<DateOnly, long>> CountLoginActivityPerDay(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var emailLogins = await emailLoginRepository.GetCompletedSinceAsync(since, cancellationToken);
        var externalLogins = await externalLoginRepository.GetSucceededSinceAsync(since, cancellationToken);
        var timestamps = emailLogins.Select(l => l.CreatedAt).Concat(externalLogins.Select(l => l.CreatedAt));
        return BucketByDay(timestamps);
    }

    private static Dictionary<DateOnly, long> BucketByDay(IEnumerable<DateTimeOffset> timestamps)
    {
        return timestamps
            .GroupBy(timestamp => DateOnly.FromDateTime(timestamp.UtcDateTime))
            .ToDictionary(group => group.Key, group => group.LongCount());
    }
}

public static class DashboardTrendPeriods
{
    public static int GetDays(DashboardTrendPeriod period)
    {
        return period switch
        {
            DashboardTrendPeriod.Last7Days => 7,
            DashboardTrendPeriod.Last30Days => 30,
            DashboardTrendPeriod.Last90Days => 90,
            _ => throw new UnreachableException($"Unsupported period '{period}'.")
        };
    }
}
