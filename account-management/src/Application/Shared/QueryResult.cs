using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Application.Shared;

[UsedImplicitly]
public sealed record QueryError(string Message);

public class QueryResult<T>
{
    private QueryResult(bool isSuccess, T value, QueryError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value { get; }

    public QueryError? Error { get; }

    public static QueryResult<T> Failure(string message)
    {
        return new QueryResult<T>(false, default!, new QueryError(message));
    }

    [UsedImplicitly]
    public static QueryResult<T> Success(T value)
    {
        return new QueryResult<T>(true, value, null);
    }

    public static implicit operator QueryResult<T>(T value)
    {
        return Success(value);
    }
}