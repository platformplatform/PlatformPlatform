namespace SharedKernel.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlagOverrideSource
{
    Default,
    ManualOverride,
    AbRollout
}
