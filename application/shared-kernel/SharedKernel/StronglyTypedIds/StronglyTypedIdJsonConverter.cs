using System.Text.Json;

namespace PlatformPlatform.SharedKernel.StronglyTypedIds;

public class StronglyTypedIdJsonConverter<TValue, TStronglyTypedId> : JsonConverter<TStronglyTypedId>
    where TStronglyTypedId : StronglyTypedId<TValue, TStronglyTypedId>
    where TValue : IComparable<TValue>
{
    public override TStronglyTypedId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        if (stringValue is null)
        {
            return null;
        }

        // First try the concrete type's TryParse method
        var tryParseMethod = typeToConvert.GetMethod("TryParse", [typeof(string), typeToConvert.MakeByRefType()]);
        if (tryParseMethod is not null)
        {
            var parameters = new object?[] { stringValue, null };
            if ((bool)tryParseMethod.Invoke(null, parameters)!)
            {
                return (TStronglyTypedId?)parameters[1];
            }
        }

        // If that fails, try the base type's TryParse method
        var baseType = typeToConvert.BaseType;
        var baseTryParseMethod = baseType?.GetMethod("TryParse", [typeof(string), typeToConvert.MakeByRefType()]);
        if (baseTryParseMethod is not null)
        {
            var parameters = new object?[] { stringValue, null };
            if ((bool)baseTryParseMethod.Invoke(null, parameters)!)
            {
                return (TStronglyTypedId?)parameters[1];
            }
        }

        throw new JsonException($"Unable to convert {typeof(TStronglyTypedId).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TStronglyTypedId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
