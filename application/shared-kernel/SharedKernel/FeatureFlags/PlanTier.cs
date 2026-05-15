namespace SharedKernel.FeatureFlags;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlanTier
{
    Free = 0,
    Standard = 1,
    Premium = 2
}
