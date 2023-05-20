namespace PlatformPlatform.Foundation.DomainModeling.Validation;

public sealed class ValidationResult
{
    private ValidationResult(AttributeError[]? errors)
    {
        Errors = errors ?? Array.Empty<AttributeError>();
    }

    public AttributeError[] Errors { get; }

    public static ValidationResult Success()
    {
        return new ValidationResult(null);
    }

    public static ValidationResult Failure(string name, string error)
    {
        return new ValidationResult(new[] {new AttributeError(name, error)});
    }
}