namespace PlatformPlatform.Foundation.DomainModeling.Validation;

public sealed class ValidationStatus
{
    private ValidationStatus(AttributeError[]? errors)
    {
        Errors = errors ?? Array.Empty<AttributeError>();
    }

    public AttributeError[] Errors { get; }

    public static ValidationStatus Success()
    {
        return new ValidationStatus(null);
    }

    public static ValidationStatus Failure(string name, string error)
    {
        return new ValidationStatus(new[] {new AttributeError(name, error)});
    }
}