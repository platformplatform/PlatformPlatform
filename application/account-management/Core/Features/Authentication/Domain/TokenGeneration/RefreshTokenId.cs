using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain.TokenGeneration;

[PublicAPI]
[IdPrefix("rt")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, RefreshTokenId>))]
public sealed record RefreshTokenId(string Value) : StronglyTypedUlid<RefreshTokenId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
