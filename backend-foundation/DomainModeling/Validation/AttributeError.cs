using JetBrains.Annotations;

namespace PlatformPlatform.Foundation.DomainModeling.Validation;

[UsedImplicitly]
public sealed record AttributeError(string? PropertyName, string Message)
{
    private readonly string? _propertyName = PropertyName;

    public string? PropertyName
    {
        get => _propertyName?.Split('.').First();
        init => _propertyName = value;
    }
}