using Account.Features.Authentication.Domain;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.ExternalAuthentication.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Users.BackOffice.Queries;

[PublicAPI]
public sealed record GetBackOfficeUserLoginHistoryQuery : IRequest<Result<BackOfficeUserLoginHistoryResponse>>
{
    [JsonIgnore] // Removes from API contract
    public UserId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record BackOfficeUserLoginHistoryResponse(BackOfficeUserLoginEntry[] Entries);

[PublicAPI]
public sealed record BackOfficeUserLoginEntry(
    LoginEventKind Kind,
    LoginMethod Method,
    LoginEventOutcome Outcome,
    DateTimeOffset OccurredAt,
    string? FailureReason,
    ExternalProviderType? ExternalProvider
);

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoginEventKind
{
    Email,
    External
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LoginEventOutcome
{
    Pending,
    Succeeded,
    Failed
}

public sealed class GetBackOfficeUserLoginHistoryHandler(
    IUserRepository userRepository,
    IEmailLoginRepository emailLoginRepository,
    IExternalLoginRepository externalLoginRepository,
    TimeProvider timeProvider
) : IRequestHandler<GetBackOfficeUserLoginHistoryQuery, Result<BackOfficeUserLoginHistoryResponse>>
{
    private const int LookbackDays = 30;

    public async Task<Result<BackOfficeUserLoginHistoryResponse>> Handle(GetBackOfficeUserLoginHistoryQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (user is null)
        {
            return Result<BackOfficeUserLoginHistoryResponse>.NotFound($"User with id '{query.Id}' was not found.");
        }

        // The PRD calls this section "real sign-in attempts" - so we union both authentication aggregates by email.
        // The aggregates don't track IP or country today; back-office shows only what we have until those columns land.
        var since = timeProvider.GetUtcNow().AddDays(-LookbackDays);
        var emailLogins = await emailLoginRepository.GetByEmailSinceAsync(user.Email, since, cancellationToken);
        var externalLogins = await externalLoginRepository.GetByEmailSinceAsync(user.Email, since, cancellationToken);

        var entries = new List<BackOfficeUserLoginEntry>(emailLogins.Length + externalLogins.Length);

        entries.AddRange(emailLogins.Select(e => new BackOfficeUserLoginEntry(
                    LoginEventKind.Email,
                    LoginMethod.OneTimePassword,
                    MapEmailOutcome(e),
                    e.CreatedAt,
                    MapEmailFailureReason(e),
                    null
                )
            )
        );

        entries.AddRange(externalLogins.Select(e => new BackOfficeUserLoginEntry(
                    LoginEventKind.External,
                    MapExternalMethod(e.ProviderType),
                    MapExternalOutcome(e.LoginResult),
                    e.CreatedAt,
                    e.LoginResult is null or ExternalLoginResult.Success ? null : e.LoginResult.ToString(),
                    e.ProviderType
                )
            )
        );

        var ordered = entries.OrderByDescending(e => e.OccurredAt).ToArray();
        return new BackOfficeUserLoginHistoryResponse(ordered);
    }

    private static LoginEventOutcome MapEmailOutcome(EmailLogin login)
    {
        if (login.Completed) return LoginEventOutcome.Succeeded;
        if (login.RetryCount >= EmailLogin.MaxAttempts) return LoginEventOutcome.Failed;
        return LoginEventOutcome.Pending;
    }

    private static string? MapEmailFailureReason(EmailLogin login)
    {
        if (login.Completed) return null;
        if (login.RetryCount >= EmailLogin.MaxAttempts) return "TooManyRetries";
        return null;
    }

    private static LoginEventOutcome MapExternalOutcome(ExternalLoginResult? result)
    {
        return result switch
        {
            null => LoginEventOutcome.Pending,
            ExternalLoginResult.Success => LoginEventOutcome.Succeeded,
            _ => LoginEventOutcome.Failed
        };
    }

    private static LoginMethod MapExternalMethod(ExternalProviderType providerType)
    {
        return providerType switch
        {
            ExternalProviderType.Google => LoginMethod.Google,
            _ => throw new UnreachableException($"Unknown external provider type '{providerType}'.")
        };
    }
}
