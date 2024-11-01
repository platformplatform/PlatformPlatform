using System.Globalization;

namespace PlatformPlatform.SharedKernel.IdGenerators;

public class StronglyTypedIdTypeConverter<TValue, T> : TypeConverter
    where T : StronglyTypedId<TValue, T> where TValue : IComparable<TValue>
{
    private static readonly MethodInfo? TryParseMethod = typeof(T).GetMethod("TryParse");

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string) && TryParseMethod != null)
        {
            return true;
        }

        return base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string valueAsString || TryParseMethod is null)
        {
            return base.ConvertFrom(context, culture, value);
        }

        var parameters = new object?[] { valueAsString, null };

        if ((bool)TryParseMethod.Invoke(null, parameters)!)
        {
            return (T?)parameters[1];
        }

        return base.ConvertFrom(context, culture, value);
    }
}
