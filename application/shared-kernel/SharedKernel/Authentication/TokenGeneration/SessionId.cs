using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

[PublicAPI]
[IdPrefix("sess")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, SessionId>))]
public sealed record SessionId(string Value) : StronglyTypedUlid<SessionId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
