using System.Security;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Domain;

public sealed class ExternalLogin : AggregateRoot<ExternalLoginId>
{
    public const int ValidForSeconds = 300;

    private ExternalLogin(
        ExternalLoginType type,
        ExternalProviderType providerType,
        string codeVerifier,
        string nonce,
        string browserFingerprint
    )
        : base(ExternalLoginId.NewId())
    {
        Type = type;
        ProviderType = providerType;
        CodeVerifier = codeVerifier;
        Nonce = nonce;
        BrowserFingerprint = browserFingerprint;
    }

    public ExternalLoginType Type { get; private init; }

    public ExternalProviderType ProviderType { get; private init; }

    public string? Email { get; private set; }

    public string CodeVerifier { get; private init; }

    public string Nonce { get; private init; }

    // Stored for forensic analysis only; validation uses the cookie copy for CSRF binding
    public string BrowserFingerprint { get; private init; }

    public ExternalLoginResult? LoginResult { get; private set; }

    public bool IsConsumed => LoginResult is not null;

    public bool IsExpired(DateTimeOffset now)
    {
        if (CreatedAt > now)
        {
            throw new SecurityException($"ExternalLogin '{Id}' has CreatedAt in the future. Possible data tampering.");
        }

        return now > CreatedAt.AddSeconds(ValidForSeconds);
    }

    public static ExternalLogin Create(
        ExternalLoginType type,
        ExternalProviderType providerType,
        string codeVerifier,
        string nonce,
        string browserFingerprint
    )
    {
        return new ExternalLogin(type, providerType, codeVerifier, nonce, browserFingerprint);
    }

    public void MarkCompleted(string email)
    {
        if (LoginResult is not null)
        {
            throw new UnreachableException("The external login has already been completed.");
        }

        Email = email;
        LoginResult = ExternalLoginResult.Success;
    }

    public void MarkFailed(ExternalLoginResult loginResult)
    {
        if (loginResult == ExternalLoginResult.Success)
        {
            throw new UnreachableException("Cannot mark a login as failed with a success result.");
        }

        if (LoginResult is not null)
        {
            throw new UnreachableException("The external login has already been completed.");
        }

        LoginResult = loginResult;
    }
}

[PublicAPI]
[IdPrefix("exlog")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, ExternalLoginId>))]
public sealed record ExternalLoginId(string Value) : StronglyTypedUlid<ExternalLoginId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
