using System.Net;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

/// <summary>
///     All commands and queries returns a <see cref="Result{T}" />. This is used to indicate if the command/query was
///     successful or not.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public interface IResult
{
    bool IsSuccess { get; }

    ErrorMessage? ErrorMessage { get; }

    ErrorDetail[]? Errors { get; }

    HttpStatusCode StatusCode { get; }
}

/// <summary>
///     The Result class is the concrete implementation of <see cref="IResult" />. In the case of the success the result
///     will contain a Value or a NoContent status. In the case of a failure, the value will be null and contain either
///     an <see cref="ErrorMessage" /> or a collection of a <see cref="ErrorMessage" />. In both successful and
///     non-successful cases, the <see cref="Result{T}" /> will contain a status code.
/// </summary>
public class Result<T> : IResult
{
    private Result(T value, HttpStatusCode httpStatusCode)
    {
        Value = value;
        IsSuccess = true;
        StatusCode = httpStatusCode;
    }

    [UsedImplicitly]
    public Result(HttpStatusCode statusCode, ErrorMessage errorMessage, ErrorDetail[] errors)
    {
        IsSuccess = false;
        StatusCode = statusCode;
        ErrorMessage = errorMessage;
        Errors = errors;
    }

    public T? Value { get; }

    public bool IsSuccess { get; }

    public HttpStatusCode StatusCode { get; }

    public ErrorMessage? ErrorMessage { get; }

    public ErrorDetail[]? Errors { get; }

    public string GetErrorSummary()
    {
        return ErrorMessage?.Message
               ?? string.Join(Environment.NewLine, Errors!.Select(ed => $"{ed.Code}: {ed.Message}"));
    }

    public static Result<T> NotFound(string message)
    {
        return new Result<T>(HttpStatusCode.NotFound, new ErrorMessage(message), Array.Empty<ErrorDetail>());
    }

    public static Result<T> BadRequest(string message)
    {
        return new Result<T>(HttpStatusCode.BadRequest, new ErrorMessage(message), Array.Empty<ErrorDetail>());
    }

    public static Result<T> NoContent()
    {
        return new Result<T>(default!, HttpStatusCode.NoContent);
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