using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace PlatformPlatform.SharedKernel.DomainCore.Identity;

public abstract class StronglyTypedIdTypeConverter<T> : TypeConverter where T : StronglyTypedLongId<T>
{
    private static readonly MethodInfo? TryParseMethod = typeof(T).GetMethod("TryParse");

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string valueAsString || TryParseMethod == null)
        {
            return base.ConvertFrom(context, culture, value);
        }

        var parameters = new object?[] {valueAsString, null};

        if ((bool) TryParseMethod.Invoke(null, parameters)!)
        {
            return (T?) parameters[1];
        }

        return base.ConvertFrom(context, culture, value);
    }
}