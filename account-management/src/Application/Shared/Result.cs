namespace PlatformPlatform.AccountManagement.Application.Shared;

public sealed class Result<T>
{
    private Result(bool isSuccess, T value, List<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; private set; }

    public T Value { get; private set; }

    public List<string> Errors { get; private set; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, new List<string>());
    }

    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>(false, default!, errors);
    }
}