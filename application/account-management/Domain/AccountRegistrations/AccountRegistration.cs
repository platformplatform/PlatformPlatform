using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public sealed class AccountRegistration : AggregateRoot<AccountRegistrationId>
{
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

    public TenantId? TenantId { get; private set; }

    [UsedImplicitly]
    public DateTimeOffset ValidUntil { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    private string GenerateOneTimePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
    }

    public bool IsValid()
    {
        return ValidUntil > TimeProvider.System.GetUtcNow();
    }

    public static AccountRegistration Create(string email, string firstName, string lastName)
    {
        return new AccountRegistration(email.ToLowerInvariant(), firstName, lastName);
    }

    public void MarkAsComplete()
    {
        if (!IsValid()) throw new InvalidOperationException("This account registration has expired.");
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