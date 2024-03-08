using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public sealed class AccountRegistration : AggregateRoot<AccountRegistrationId>
{
    public const int MaxAttempts = 3;

    private static readonly Random Random = new();

    private AccountRegistration(TenantId tenantId, string email) : base(AccountRegistrationId.NewId())
    {
        TenantId = tenantId;
        Email = email;
        OneTimePassword = GenerateOneTimePassword(6);
        ValidUntil = CreatedAt.AddMinutes(5);
    }

    public TenantId TenantId { get; private set; }

    public string Email { get; private set; }

    public string OneTimePassword { get; private set; }

    public int RetryCount { get; private set; }

    [UsedImplicitly]
    public DateTimeOffset ValidUntil { get; private set; }

    public bool Completed { get; private set; }

    private string GenerateOneTimePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    public bool HasExpired()
    {
        return ValidUntil < TimeProvider.System.GetUtcNow();
    }

    public static AccountRegistration Create(TenantId tenantId, string email)
    {
        return new AccountRegistration(tenantId, email.ToLowerInvariant());
    }

    public void RegisterInvalidPasswordAttempt()
    {
        RetryCount++;
    }

    public void MarkAsCompleted()
    {
        if (HasExpired() || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This account registration has expired.");
        }

        if (Completed) throw new UnreachableException("The account has already been created.");

        Completed = true;
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