using System.Security.Cryptography;
using System.Text;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Stable SHA-256 hash of a raw Stripe webhook payload. Used by AcknowledgeStripeWebhook on insert
///     and by the reconciliation pass on recovered rows so the same value lands in stripe_events.payload_hash
///     regardless of the entry path. Comparing hashes is how StripeEventPayloadDivergence detects the
///     forensic anomaly of a redelivered event with a different body.
///     Hex-lower so the same hash format works for grep/log pivots regardless of Stripe API version.
/// </summary>
public static class StripeEventPayloadHasher
{
    public static string Hash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(bytes);
    }
}
