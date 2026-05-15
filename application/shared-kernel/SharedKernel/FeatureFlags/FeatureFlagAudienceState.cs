namespace SharedKernel.FeatureFlags;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlagAudienceState
{
    Enabled,
    Disabled
}
