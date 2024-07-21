using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public sealed class AccountRegistration : AggregateRoot<AccountRegistrationId>
{
    public const int MaxAttempts = 3;
    private const int ValidForSeconds = 300;

    private AccountRegistration(TenantId tenantId, string email, string oneTimePasswordHash)
        : base(AccountRegistrationId.NewId())
    {
        TenantId = tenantId;
        Email = email;
        OneTimePasswordHash = oneTimePasswordHash;
        ValidUntil = CreatedAt.AddSeconds(ValidForSeconds);
    }

    public TenantId TenantId { get; private set; }

    public string Email { get; private set; }

    public string OneTimePasswordHash { get; private set; }

    [UsedImplicitly]
    public DateTimeOffset ValidUntil { get; private set; }

    public int RetryCount { get; private set; }

    public bool Completed { get; private set; }

    public bool HasExpired()
    {
        return ValidUntil < TimeProvider.System.GetUtcNow();
    }

    public static AccountRegistration Create(TenantId tenantId, string email, string oneTimePasswordHash)
    {
        return new AccountRegistration(tenantId, email.ToLowerInvariant(), oneTimePasswordHash);
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

    public int GetValidForSeconds()
    {
        return Convert.ToInt16((ValidUntil - TimeProvider.System.GetUtcNow()).TotalSeconds);
    }
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, AccountRegistrationId>))]
[IdPrefix("accreg")]
public sealed record AccountRegistrationId(string Value) : StronglyTypedUlid<AccountRegistrationId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
