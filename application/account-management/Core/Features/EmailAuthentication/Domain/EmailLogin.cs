using System.Security;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;

public sealed class EmailLogin : AggregateRoot<EmailLoginId>
{
    public const int MaxAttempts = 3;
    public const int MaxResends = 1;
    public const int ValidForSeconds = 300;

    private EmailLogin(string email, EmailLoginType type, string oneTimePasswordHash)
        : base(EmailLoginId.NewId())
    {
        Email = email;
        Type = type;
        OneTimePasswordHash = oneTimePasswordHash;
    }

    public string Email { get; private set; }

    public EmailLoginType Type { get; private set; }

    public string OneTimePasswordHash { get; private set; }

    public int RetryCount { get; private set; }

    public int ResendCount { get; private set; }

    public bool Completed { get; private set; }

    public bool IsExpired(DateTimeOffset now)
    {
        if (CreatedAt > now)
        {
            throw new SecurityException($"EmailLogin '{Id}' has CreatedAt in the future. Possible data tampering.");
        }

        if (CreatedAt.AddSeconds(ValidForSeconds * (MaxResends + 1)) < now) return true;
        if ((ModifiedAt ?? CreatedAt).AddSeconds(ValidForSeconds) < now) return true;
        return false;
    }

    public static EmailLogin Create(string email, string oneTimePasswordHash, EmailLoginType type)
    {
        return new EmailLogin(email.ToLowerInvariant(), type, oneTimePasswordHash);
    }

    public void RegisterInvalidPasswordAttempt()
    {
        RetryCount++;
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        if (IsExpired(now) || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This email login has expired.");
        }

        if (Completed) throw new UnreachableException("The email login has already been completed.");

        Completed = true;
    }

    public void UpdateVerificationCode(string oneTimePasswordHash, DateTimeOffset now)
    {
        if (Completed)
        {
            throw new UnreachableException("Cannot regenerate verification code for completed email login.");
        }

        if (ResendCount >= MaxResends)
        {
            throw new UnreachableException("Cannot regenerate verification code for email login that has been resent too many times.");
        }

        OneTimePasswordHash = oneTimePasswordHash;
        ResendCount++;
    }
}

[PublicAPI]
[IdPrefix("emlog")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, EmailLoginId>))]
public sealed record EmailLoginId(string Value) : StronglyTypedUlid<EmailLoginId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
