using System.Security;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;

public sealed class ExternalLogin : AggregateRoot<ExternalLoginId>
{
    public const int ValidForSeconds = 300;

    private ExternalLogin(
        ExternalProviderType providerType,
        ExternalLoginType type,
        string codeVerifier,
        string nonce,
        string browserFingerprint
    )
        : base(ExternalLoginId.NewId())
    {
        ProviderType = providerType;
        Type = type;
        CodeVerifier = codeVerifier;
        Nonce = nonce;
        BrowserFingerprint = browserFingerprint;
    }

    public ExternalProviderType ProviderType { get; private init; }

    public ExternalLoginType Type { get; private init; }

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
        ExternalProviderType providerType,
        ExternalLoginType type,
        string codeVerifier,
        string nonce,
        string browserFingerprint
    )
    {
        return new ExternalLogin(providerType, type, codeVerifier, nonce, browserFingerprint);
    }

    public void MarkCompleted()
    {
        if (LoginResult is not null)
        {
            throw new UnreachableException("The external login has already been completed.");
        }

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
