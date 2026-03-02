using SharedKernel.StronglyTypedIds;

namespace SharedKernel.Authentication.TokenGeneration;

[PublicAPI]
[IdPrefix("jti")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, RefreshTokenJti>))]
public sealed record RefreshTokenJti(string Value) : StronglyTypedUlid<RefreshTokenJti>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
