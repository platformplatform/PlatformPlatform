namespace Account.Features.FeatureFlags.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlagSource
{
    Manual,
    Plan,
    AbRollout
}
