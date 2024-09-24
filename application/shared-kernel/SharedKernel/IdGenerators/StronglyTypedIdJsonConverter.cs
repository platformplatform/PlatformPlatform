using System.Text.Json;

namespace PlatformPlatform.SharedKernel.IdGenerators;

public class StronglyTypedIdJsonConverter<TValue, TStronglyTypedId> : JsonConverter<TStronglyTypedId>
    where TStronglyTypedId : StronglyTypedId<TValue, TStronglyTypedId> where TValue : IComparable<TValue>
{
    public override TStronglyTypedId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = TypeDescriptor.GetConverter(typeof(TStronglyTypedId));

        if (!converter.CanConvertFrom(typeof(string))) throw new JsonException($"Unable to convert {typeof(TStronglyTypedId).Name}.");

        var stringValue = reader.GetString();
        return stringValue is null ? null : converter.ConvertFrom(stringValue) as TStronglyTypedId;
    }

    public override void Write(Utf8JsonWriter writer, TStronglyTypedId value, JsonSerializerOptions options)
    {
        var converter = TypeDescriptor.GetConverter(typeof(TStronglyTypedId));

        if (!converter.CanConvertTo(typeof(string)))
        {
            throw new JsonException($"Unable to convert {typeof(TStronglyTypedId).Name} to JSON.");
        }

        var stringValue = converter.ConvertToString(value);
        writer.WriteStringValue(stringValue);
    }
}
