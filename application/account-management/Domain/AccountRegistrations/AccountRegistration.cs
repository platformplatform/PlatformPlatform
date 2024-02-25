using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public sealed class AccountRegistration : AggregateRoot<AccountRegistrationId>
{
    public const int MaxAttempts = 3;

    private static readonly Random Random = new();

    private AccountRegistration(string email, string firstName, string lastName) : base(AccountRegistrationId.NewId())
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        OneTimePassword = GenerateOneTimePassword(6);
        ValidUntil = CreatedAt.AddMinutes(5);
    }

    public string Email { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string OneTimePassword { get; private set; }

    public int RetryCount { get; private set; }

    public TenantId? TenantId { get; private set; }

    [UsedImplicitly]
    public DateTimeOffset ValidUntil { get; private set; }

    public DateTimeOffset? EmailConfirmedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    private string GenerateOneTimePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    public bool HasExpired()
    {
        return ValidUntil < TimeProvider.System.GetUtcNow();
    }

    public static AccountRegistration Create(string email, string firstName, string lastName)
    {
        return new AccountRegistration(email.ToLowerInvariant(), firstName, lastName);
    }

    public void RegisterInvalidPasswordAttempt()
    {
        RetryCount++;
    }

    public void ConfirmEmail()
    {
        if (HasExpired() || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This account registration has expired.");
        }

        if (EmailConfirmedAt.HasValue) throw new UnreachableException("The mail confirmation already occured.");
        EmailConfirmedAt = TimeProvider.System.GetUtcNow();
    }

    public void MarkAsCompleted(TenantId tenantId)
    {
        if (HasExpired() || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This account registration has expired.");
        }

        if (!EmailConfirmedAt.HasValue) throw new UnreachableException("The mail is not confirmation.");
        if (CompletedAt.HasValue) throw new UnreachableException("The account has already been created.");

        TenantId = tenantId;
        CompletedAt = TimeProvider.System.GetUtcNow();
    }
}

[TypeConverter(typeof(UserIdTypeConverter))]
[UsedImplicitly]
public sealed record AccountRegistrationId(string Value) : StronglyTypedUlid<AccountRegistrationId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}