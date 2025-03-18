using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;

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
        ValidUntil = CreatedAt.AddSeconds(ValidForSeconds);
    }

    public string Email { get; private set; }

    public EmailConfirmationType Type { get; private set; }

    public string OneTimePasswordHash { get; private set; }

    [UsedImplicitly]
    public DateTimeOffset ValidUntil { get; private set; }

    public int RetryCount { get; private set; }

    public int ResendCount { get; private set; }

    public bool Completed { get; private set; }

    public bool HasExpired()
    {
        return ValidUntil < TimeProvider.System.GetUtcNow();
    }

    public static EmailConfirmation Create(string email, string oneTimePasswordHash, EmailConfirmationType type)
    {
        return new EmailConfirmation(email.ToLowerInvariant(), type, oneTimePasswordHash);
    }

    public void RegisterInvalidPasswordAttempt()
    {
        RetryCount++;
    }

    public void MarkAsCompleted()
    {
        if (HasExpired() || RetryCount >= MaxAttempts)
        {
            throw new UnreachableException("This email confirmation has expired.");
        }

        if (Completed) throw new UnreachableException("The email has already been confirmed.");

        Completed = true;
    }

    public void UpdateVerificationCode(string oneTimePasswordHash)
    {
        if (Completed)
        {
            throw new UnreachableException("Cannot regenerate verification code for completed email confirmation");
        }

        if (ResendCount >= MaxResends)
        {
            throw new UnreachableException("Cannot regenerate verification code for email confirmation that has been resent too many times.");
        }

        ValidUntil = TimeProvider.System.GetUtcNow().AddSeconds(ValidForSeconds);
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
