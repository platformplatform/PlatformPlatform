using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Application.Shared.Validation;

namespace PlatformPlatform.AccountManagement.Application.Shared;

[UsedImplicitly]
public sealed record PropertyError(string? PropertyName, string Message);

public interface ICommandResult
{
}

public class CommandResult<T> : ICommandResult
{
    private CommandResult(bool isSuccess, T value, PropertyError[]? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value { get; private set; }

    public PropertyError[]? Errors { get; }

    public static CommandResult<T> Failure(PropertyError[] errors)
    {
        return new CommandResult<T>(false, default!, errors);
    }

    [UsedImplicitly]
    public static CommandResult<T> Success(T value)
    {
        return new CommandResult<T>(true, value, null);
    }
    public static implicit operator CommandResult<T>(T value)
    {
        return new CommandResult<T>(true, value, null);
    }
}