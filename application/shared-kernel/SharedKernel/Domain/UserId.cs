using PlatformPlatform.SharedKernel.IdGenerators;

namespace PlatformPlatform.SharedKernel.Domain;

[PublicAPI]
[IdPrefix("usr")]
[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, UserId>))]
public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
