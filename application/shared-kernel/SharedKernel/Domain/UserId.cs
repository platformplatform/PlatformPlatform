using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Domain;

[PublicAPI]
[IdPrefix("usr")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, UserId>))]
public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
