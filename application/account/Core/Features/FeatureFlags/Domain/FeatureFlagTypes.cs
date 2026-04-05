namespace Account.Features.FeatureFlags.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlagSource
{
    Manual,
    SubscriptionPlan,
    AbRollout
}
