namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Per-Stripe-API-version dispatcher for the JSON-shape navigation that the replayer relies on.
///     Stripe events carry an immutable <c>api_version</c> at creation time
///     (see https://docs.stripe.com/api/events). When Stripe ships a new API version that renames
///     fields or restructures the payload, a new resolver implementation handles the new shape while
///     existing rows keep their original resolver — old fixtures keep passing without modification.
///     <see cref="StripeEventPayloadResolverFactory.For" /> throws
///     <see cref="UnsupportedStripeApiVersionException" /> for unknown versions; the calling code
///     catches it and adds a <c>UnsupportedStripeApiVersion</c> drift discrepancy so an admin knows
///     to add the new resolver.
/// </summary>
public interface IStripeEventPayloadResolver;

/// <summary>
///     Default resolver for Stripe API versions whose JSON shape matches what
///     <see cref="StripeEventReplayer" /> currently parses. Extends as new versions ship —
///     each new resolver implementation handles its own payload navigation.
/// </summary>
public sealed class DefaultStripeEventPayloadResolver : IStripeEventPayloadResolver;

/// <summary>
///     Routes a Stripe event's <c>api_version</c> to the matching resolver.
/// </summary>
public static class StripeEventPayloadResolverFactory
{
    private static readonly DefaultStripeEventPayloadResolver Default = new();

    // TODO: When Stripe rolls a new pinned API version, update this list AND add a new
    // IStripeEventPayloadResolver implementation. Future: derive from Stripe.NET's
    // StripeConfiguration.ApiVersion. Keeping it explicit for now so adding a version is a
    // deliberate, reviewable change rather than a silent dependency upgrade.
    private static readonly HashSet<string> RecognizedVersions = ["2025-09-30.preview", "2025-10-29.clover"];

    public static IStripeEventPayloadResolver For(string apiVersion)
    {
        if (!TryFor(apiVersion, out var resolver))
        {
            throw new UnsupportedStripeApiVersionException(apiVersion);
        }

        return resolver;
    }

    /// <summary>
    ///     Non-throwing variant of <see cref="For" />. Returns false when the api_version is unknown so
    ///     callers in hot paths (the replayer) can branch on the result instead of paying for an
    ///     exception. The throwing <see cref="For" /> remains for genuinely unreachable cases.
    /// </summary>
    public static bool TryFor(string apiVersion, out IStripeEventPayloadResolver resolver)
    {
        if (RecognizedVersions.Contains(apiVersion))
        {
            resolver = Default;
            return true;
        }

        resolver = null!;
        return false;
    }
}

/// <summary>
///     Thrown by <see cref="StripeEventPayloadResolverFactory.For" /> when Stripe sends an event
///     whose api_version we don't have a resolver for. The replayer catches this, logs the event,
///     and surfaces a <c>UnsupportedStripeApiVersion</c> drift discrepancy so the missing resolver
///     can be added.
/// </summary>
public sealed class UnsupportedStripeApiVersionException(string apiVersion)
    : InvalidOperationException($"Stripe event api_version '{apiVersion}' is not supported by any registered IStripeEventPayloadResolver.")
{
    public string ApiVersion { get; } = apiVersion;
}
