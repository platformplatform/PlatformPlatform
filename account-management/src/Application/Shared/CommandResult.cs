using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Application.Shared.Validation;

namespace PlatformPlatform.AccountManagement.Application.Shared;

/// <summary>
///     Use this to indicate a error when doing a command. The <see cref="CommandResult{T}" /> has a implicit
///     conversion, so you can return a PropertyError from a command handler, and it will be converted to a
///     <see cref="CommandResult{T}" />.
/// </summary>
[UsedImplicitly]
public sealed record PropertyError(string? PropertyName, string Message);

/// <summary>
///     This is a marker interface. It makes it possible to do a generic registration of all commands with the
///     <see cref="ValidationPipelineBehavior{TRequest,TResponse}" /> without using reflection.
/// </summary>
public interface ICommandResult
{
}

/// <summary>
///     All commands should return a <see cref="CommandResult{T}" />. This is used to indicate if the command was
///     successful or not. If the command was successful, the <see cref="CommandResult{T}" /> will contain the result of
///     the command. If the command was not successful, the <see cref="CommandResult{T}" /> will contain a collection of
///     <see cref="PropertyError" />.
/// </summary>
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

    /// <summary>
    ///     Use this to indicate a failed command, with a collection of <see cref="PropertyError" />. The
    ///     <see cref="ValidationPipelineBehavior{TRequest,TResponse}" /> is automatically collecting all
    ///     <see cref="PropertyError" /> and using this method to create a <see cref="CommandResult{T}" />.
    /// </summary>
    public static CommandResult<T> Failure(PropertyError[] errors)
    {
        return new CommandResult<T>(false, default!, errors);
    }

    /// <summary>
    ///     Use this to indicate a successful command. There is a implicit conversion from T to
    ///     <see cref="CommandResult{T}" />, so you can also just return T from a Command handler.
    /// </summary>
    [UsedImplicitly]
    public static CommandResult<T> Success(T value)
    {
        return new CommandResult<T>(true, value, null);
    }

    /// <summary>
    ///     This is an implicit conversion from T to <see cref="CommandResult{T}" />. This is used to easily return a
    ///     successful <see cref="CommandResult{T}" /> from a command handler.
    /// </summary>
    public static implicit operator CommandResult<T>(T value)
    {
        return new CommandResult<T>(true, value, null);
    }
}