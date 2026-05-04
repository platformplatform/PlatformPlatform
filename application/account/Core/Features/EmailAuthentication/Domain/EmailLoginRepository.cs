using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.EmailAuthentication.Domain;

public interface IEmailLoginRepository : IAppendRepository<EmailLogin, EmailLoginId>
{
    void Update(EmailLogin aggregate);

    EmailLogin[] GetByEmail(string email);

    /// <summary>
    ///     Returns every email login for the given email address created at or after <paramref name="since" />.
    ///     Used by the back-office login history endpoint to surface the full sign-in history (including failed
    ///     and pending attempts), not just the active in-progress logins returned by <see cref="GetByEmail" />.
    /// </summary>
    Task<EmailLogin[]> GetByEmailSinceAsync(string email, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns every completed email login created at or after <paramref name="since" />. Used by the back-office
    ///     dashboard to aggregate successful email login activity per day across all tenants.
    /// </summary>
    Task<EmailLogin[]> GetCompletedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken);
}

public sealed class EmailLoginRepository(AccountDbContext accountDbContext)
    : RepositoryBase<EmailLogin, EmailLoginId>(accountDbContext), IEmailLoginRepository
{
    public EmailLogin[] GetByEmail(string email)
    {
        return DbSet
            .Where(el => !el.Completed)
            .Where(el => el.Email == email.ToLowerInvariant())
            .ToArray();
    }

    /// <summary>
    ///     Returns every email login for the given email address created at or after <paramref name="since" />.
    ///     Used by the back-office login history endpoint to surface the full sign-in history (including failed
    ///     and pending attempts), not just the active in-progress logins returned by <see cref="GetByEmail" />.
    ///     SQLite cannot translate DateTimeOffset comparisons, so the time filter runs in memory; the email filter
    ///     keeps the materialized set bounded.
    /// </summary>
    public async Task<EmailLogin[]> GetByEmailSinceAsync(string email, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var logins = await DbSet
            .Where(el => el.Email == email.ToLowerInvariant())
            .ToArrayAsync(cancellationToken);
        return logins.Where(el => el.CreatedAt >= since).ToArray();
    }

    /// <summary>
    ///     Returns every completed email login created at or after <paramref name="since" />. Used by the back-office
    ///     dashboard to aggregate successful email login activity per day across all tenants. SQLite cannot translate
    ///     DateTimeOffset comparisons, so the time filter runs in memory; the dashboard period is bounded (max 90 days)
    ///     so the materialized set stays small.
    /// </summary>
    public async Task<EmailLogin[]> GetCompletedSinceAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var logins = await DbSet.Where(el => el.Completed).ToArrayAsync(cancellationToken);
        return logins.Where(el => el.CreatedAt >= since).ToArray();
    }
}
