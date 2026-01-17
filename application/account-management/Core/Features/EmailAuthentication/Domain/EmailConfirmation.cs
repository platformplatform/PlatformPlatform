using System.Security;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;

public sealed class EmailConfirmation : AggregateRoot<EmailConfirmationId>
{
    public const int MaxAttempts = 3;
    public const int MaxResends = 1;
    public const int ValidForSeconds = 300;

    private EmailConfirmation(string email, EmailConfirmationType type, string oneTimePasswordHash)
        : base(EmailConfirmationId.NewId())
    {
        Email = email;
        Type = type;
        OneTimePasswordHash = oneTimePasswordHash;
    }

    public string Email { get; private set; }

    public EmailConfirmationType Type { get; private set; }

    public string OneTimePasswordHash { get; private set; }

    public int RetryCount { get; private set; }

    public int ResendCount { get; private set; }

    public bool Completed { get; private set; }

    public bool IsExpired(DateTimeOffset now)
    {
        if (CreatedAt > now)
        {
            throw new SecurityException($"EmailConfirmation '{Id}' has CreatedAt in the future. Possible data tampering.");
        }

        if (CreatedAt.AddSeconds(ValidForSeconds * (MaxResends + 1)) < now) return true;
        if ((ModifiedAt ?? CreatedAt).AddSeconds(ValidForSeconds) < now) return true;
        return false;
    }

    public static EmailConfirmation Create(string email, string oneTimePasswordHash, EmailConfirmationType type)
    {
        return new EmailConfirmation(email.ToLowerInvariant(), type, oneTimePasswordHash);
    }

    public void RegisterInvalidPasswordAttempt()
    {
        RetryCount++;
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        if (IsExpired(now) || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This email confirmation has expired.");
        }

        if (Completed) throw new UnreachableException("The email has already been confirmed.");

        Completed = true;
    }

    public void UpdateVerificationCode(string oneTimePasswordHash, DateTimeOffset now)
    {
        if (Completed)
        {
            throw new UnreachableException("Cannot regenerate verification code for completed email confirmation");
        }

        if (ResendCount >= MaxResends)
        {
            throw new UnreachableException("Cannot regenerate verification code for email confirmation that has been resent too many times.");
        }

        OneTimePasswordHash = oneTimePasswordHash;
        ResendCount++;
    }
}

[PublicAPI]
[IdPrefix("econf")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, EmailConfirmationId>))]
public sealed record EmailConfirmationId(string Value) : StronglyTypedUlid<EmailConfirmationId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
