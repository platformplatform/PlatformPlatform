using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Domain;

[PublicAPI]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<long, TenantId>))]
public sealed record TenantId(long Value) : StronglyTypedLongId<TenantId>(Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
