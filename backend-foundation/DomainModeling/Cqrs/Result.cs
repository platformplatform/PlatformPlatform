using System.Net;
using JetBrains.Annotations;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.Foundation.DomainModeling.Cqrs;

/// <summary>
///     All commands should return a <see cref="Result{T}" />. This is used to indicate if the command was
///     successful or not. If the command was successful, the <see cref="Result{T}" /> will contain the result of
///     the command. If the command was not successful, it will contain a collection of <see cref="AttributeError" />.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }

    [UsedImplicitly]
    ErrorMessage? ErrorMessage { get; }

    [UsedImplicitly]
    AttributeError[] Errors { get; }

    [UsedImplicitly]
    HttpStatusCode StatusCode { get; }
}

public class Result<T> : IResult
{
    private Result(T value, HttpStatusCode httpStatusCode)
    {
        IsSuccess = true;
        Value = value;
        StatusCode = httpStatusCode;
    }

    [UsedImplicitly]
    public Result(ErrorMessage errorMessage, AttributeError[] errors, HttpStatusCode statusCode)
    {
        IsSuccess = false;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    public T? Value { get; }

    public bool IsSuccess { get; }

    public ErrorMessage? ErrorMessage { get; }

    public AttributeError[] Errors { get; } = Array.Empty<AttributeError>();

    public HttpStatusCode StatusCode { get; }

    public static Result<T> NotFound(string message)
    {
        return new Result<T>(new ErrorMessage(message), Array.Empty<AttributeError>(), HttpStatusCode.NotFound);
    }

    public static Result<T> NoContent()
    {
        return new Result<T>(default!, HttpStatusCode.NoContent);
    }

    public static Task<Result<T>> Created(T value)
    {
        return Task.FromResult(new Result<T>(value, HttpStatusCode.Created));
    }

    public static Result<T> BadRequest(string message)
    {
        return new Result<T>(new ErrorMessage(message), Array.Empty<AttributeError>(), HttpStatusCode.BadRequest);
    }

    /// <summary>
    ///     Use this to indicate a successful command. There is a implicit conversion from T to
    ///     <see cref="Result{T}" />, so you can also just return T from a Command handler.
    /// </summary>
    public static Result<T> Success(T value)
    {
        return new Result<T>(value, HttpStatusCode.OK);
    }

    /// <summary>
    ///     This is an implicit conversion from T to <see cref="Result{T}" />. This is used to easily return a
    ///     successful <see cref="Result{T}" /> from a command handler.
    /// </summary>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }
}