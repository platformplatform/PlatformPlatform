using JetBrains.Annotations;

namespace PlatformPlatform.Foundation.DddCqrsFramework.Validation;

[UsedImplicitly]
public sealed record PropertyError(string? PropertyName, string Message);

public sealed class ValidationResult
{
    private ValidationResult(PropertyError[]? errors)
    {
        Errors = errors ?? Array.Empty<PropertyError>();
    }

    public PropertyError[] Errors { get; }

    public static ValidationResult Success()
    {
        return new ValidationResult(null);
    }

    public static ValidationResult Failure(string name, string error)
    {
        return new ValidationResult(new[] {new PropertyError(name, error)});
    }
}