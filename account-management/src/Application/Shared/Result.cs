namespace PlatformPlatform.AccountManagement.Application.Shared;

public class Result<T>
{
    private Result(bool isSuccess, T value, string[] errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; private set; }

    public T Value { get; private set; }

    public string[] Errors { get; private set; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, Array.Empty<string>());
    }

    public static Result<T> Failure(string[] errors)
    {
        return new Result<T>(false, default!, errors);
    }
}