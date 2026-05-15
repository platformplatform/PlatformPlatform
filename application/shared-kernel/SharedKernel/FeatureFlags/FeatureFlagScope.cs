namespace SharedKernel.FeatureFlags;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlagScope
{
    System,
    Tenant,
    User
}
