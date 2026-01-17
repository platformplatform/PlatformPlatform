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
        ExternalFlowType flowType,
        string stateToken,
        string codeVerifier,
        string browserFingerprint,
        string? returnPath,
        string? locale
    )
        : base(ExternalLoginId.NewId())
    {
        ProviderType = providerType;
        FlowType = flowType;
        StateToken = stateToken;
        CodeVerifier = codeVerifier;
        BrowserFingerprint = browserFingerprint;
        ReturnPath = returnPath;
        Locale = locale;
    }

    public ExternalProviderType ProviderType { get; private init; }

    public ExternalFlowType FlowType { get; private init; }

    public string StateToken { get; private set; }

    public string CodeVerifier { get; private init; }

    public string BrowserFingerprint { get; private init; }

    public string? ReturnPath { get; private init; }

    public string? Locale { get; private init; }

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
        ExternalFlowType flowType,
        string codeVerifier,
        string browserFingerprint,
        string? returnPath,
        string? locale
    )
    {
        return new ExternalLogin(providerType, flowType, string.Empty, codeVerifier, browserFingerprint, returnPath, locale);
    }

    public void SetStateToken(string stateToken)
    {
        if (!string.IsNullOrEmpty(StateToken))
        {
            throw new UnreachableException("State token has already been set.");
        }

        StateToken = stateToken;
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
