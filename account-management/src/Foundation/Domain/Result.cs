using JetBrains.Annotations;

namespace PlatformPlatform.Foundation.Domain;

[UsedImplicitly]
public sealed record PropertyError(string? PropertyName, string Message);

public sealed class Result
{
    private Result(PropertyError[]? errors)
    {
        Errors = errors ?? Array.Empty<PropertyError>();
    }

    public PropertyError[] Errors { get; }

    public static Result Success()
    {
        return new Result(null);
    }

    public static Result Failure(string name, string error)
    {
        return new Result(new[] {new PropertyError(name, error)});
    }
}