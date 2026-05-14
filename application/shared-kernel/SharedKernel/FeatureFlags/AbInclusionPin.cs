namespace SharedKernel.FeatureFlags;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AbInclusionPin
{
    AlwaysOn = 0,
    NeverOn = 1
}
