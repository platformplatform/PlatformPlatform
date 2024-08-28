using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.SharedKernel.Entities;
using PlatformPlatform.SharedKernel.Identity;

namespace PlatformPlatform.AccountManagement.Core.Signups.Domain;

public sealed class Signup : AggregateRoot<SignupId>
{
    public const int MaxAttempts = 3;
    private const int ValidForSeconds = 300;

    private Signup(TenantId tenantId, string email, string oneTimePasswordHash)
        : base(SignupId.NewId())
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

    public static Signup Create(TenantId tenantId, string email, string oneTimePasswordHash)
    {
        return new Signup(tenantId, email.ToLowerInvariant(), oneTimePasswordHash);
    }

    public void RegisterInvalidPasswordAttempt()
    {
        RetryCount++;
    }

    public void MarkAsCompleted()
    {
        if (HasExpired() || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This signup has expired.");
        }

        if (Completed) throw new UnreachableException("The account has already been created.");

        Completed = true;
    }

    public int GetValidForSeconds()
    {
        return Convert.ToInt16((ValidUntil - TimeProvider.System.GetUtcNow()).TotalSeconds);
    }
}

[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, SignupId>))]
[IdPrefix("signup")]
public sealed record SignupId(string Value) : StronglyTypedUlid<SignupId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
