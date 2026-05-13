namespace Account.Features.FeatureFlags.Domain;

// JsonStringEnumMemberName preserves the snake-case wire vocabulary that the frontend already filters on,
// while letting C# callers use the enum directly. The `Default` member has no domain row counterpart —
// it is only used as a wire value when no override, plan grant, or A/B rollout applies.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlagSource
{
    [JsonStringEnumMemberName("manual_override")]
    Manual,

    [JsonStringEnumMemberName("plan")] Plan,

    [JsonStringEnumMemberName("ab_rollout")]
    AbRollout,

    [JsonStringEnumMemberName("default")] Default
}
