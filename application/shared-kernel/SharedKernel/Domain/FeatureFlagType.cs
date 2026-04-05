namespace SharedKernel.Domain;

[JsonConverter(typeof(JsonStringEnumConverter<FeatureFlagType>))]
public enum FeatureFlagType
{
    System,
    Tenant,
    User,
    SubscriptionPlan
}
