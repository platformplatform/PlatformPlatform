using System.Net;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

public abstract class ResultBase
{
    protected ResultBase(HttpStatusCode httpStatusCode)
    {
        IsSuccess = true;
        StatusCode = httpStatusCode;
    }

    protected ResultBase(HttpStatusCode statusCode, ErrorMessage errorMessage, ErrorDetail[] errors)
    {
        IsSuccess = false;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public HttpStatusCode StatusCode { get; }

    public ErrorMessage? ErrorMessage { get; }

    public ErrorDetail[]? Errors { get; }

    public string GetErrorSummary()
    {
        return ErrorMessage?.Message
               ?? string.Join(Environment.NewLine, Errors!.Select(ed => $"{ed.Code}: {ed.Message}"));
    }
}

/// <summary>
///     The Result class is used when a successful result is not returning any value (e.g. in the case of an Update or
///     Delete). On success the HttpStatusCode NoContent will be returned. In the case of a failure, the result will
///     contain either an <see cref="ErrorMessage" /> or a collection of a <see cref="ErrorMessage" />.
/// </summary>
public sealed class Result : ResultBase
{
    private Result(HttpStatusCode httpStatusCode) : base(httpStatusCode)
    {
    }

    [UsedImplicitly]
    public Result(HttpStatusCode statusCode, ErrorMessage errorMessage, ErrorDetail[] errors)
        : base(statusCode, errorMessage, errors)
    {
    }

    public static Result NotFound(string message)
    {
        return new Result(HttpStatusCode.NotFound, new ErrorMessage(message), Array.Empty<ErrorDetail>());
    }

    [UsedImplicitly]
    public static Result BadRequest(string message)
    {
        return new Result(HttpStatusCode.BadRequest, new ErrorMessage(message), Array.Empty<ErrorDetail>());
    }

    public static Result Success()
    {
        return new Result(HttpStatusCode.NoContent);
    }
}

/// <summary>
///     The ResultT class is used when a successful command or query is returning value (e.g. in the case of an Get or
///     Create). On success the HttpStatusCode OK will be returned. In the case of a failure, the result will
///     contain either an <see cref="ErrorMessage" /> or a collection of a <see cref="ErrorMessage" />.
/// </summary>
public sealed class Result<T> : ResultBase
{
    private Result(T value, HttpStatusCode httpStatusCode) : base(httpStatusCode)
    {
        Value = value;
    }

    [UsedImplicitly]
    public Result(HttpStatusCode statusCode, ErrorMessage errorMessage, ErrorDetail[] errors)
        : base(statusCode, errorMessage, errors)
    {
    }

    public T? Value { get; }

    public static Result<T> NotFound(string message)
    {
        return new Result<T>(HttpStatusCode.NotFound, new ErrorMessage(message), Array.Empty<ErrorDetail>());
    }

    public static Result<T> BadRequest(string message)
    {
        return new Result<T>(HttpStatusCode.BadRequest, new ErrorMessage(message), Array.Empty<ErrorDetail>());
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