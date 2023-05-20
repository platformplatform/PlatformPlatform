using System.Net;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.Foundation.DomainModeling.Cqrs;

/// <summary>
///     All commands should return a <see cref="CommandResult{T}" />. This is used to indicate if the command was
///     successful or not. If the command was successful, the <see cref="CommandResult{T}" /> will contain the result of
///     the command. If the command was not successful, it will contain a collection of <see cref="AttributeError" />.
/// </summary>
public sealed class CommandResult<T>
{
    private CommandResult(bool isSuccess, T? value, AttributeError[] errors, HttpStatusCode statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        StatusCode = statusCode;
        Errors = errors;
    }

    private CommandResult(bool isSuccess, ErrorMessage errorMessage, HttpStatusCode statusCode)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        Errors = Array.Empty<AttributeError>();
    }

    public bool IsSuccess { get; }

    public T? Value { get; private set; }

    public HttpStatusCode StatusCode { get; private set; }

    public ErrorMessage? ErrorMessage { get; }

    public AttributeError[] Errors { get; }

    /// <summary>
    ///     Use this to indicate a error when doing a query.
    /// </summary>
    public static CommandResult<T> Failure(string message, HttpStatusCode statusCode)
    {
        return new CommandResult<T>(false, new ErrorMessage(message), statusCode);
    }

    /// <summary>
    ///     Use this to indicate a failed command, with a collection of <see cref="AttributeError" />.
    /// </summary>
    public static CommandResult<T> Failure(AttributeError[] errors, HttpStatusCode statusCode)
    {
        return new CommandResult<T>(false, default!, errors, statusCode);
    }

    /// <summary>
    ///     Use this to indicate a successful command. There is a implicit conversion from T to
    ///     <see cref="CommandResult{T}" />, so you can also just return T from a Command handler.
    /// </summary>
    public static CommandResult<T> Success(T? value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new CommandResult<T>(true, value, Array.Empty<AttributeError>(), statusCode);
    }

    /// <summary>
    ///     This is an implicit conversion from T to <see cref="CommandResult{T}" />. This is used to easily return a
    ///     successful <see cref="CommandResult{T}" /> from a command handler.
    /// </summary>
    public static implicit operator CommandResult<T>(T value)
    {
        return Success(value);
    }
}