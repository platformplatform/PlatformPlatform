using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Application.Shared;

public sealed record ValidationError([UsedImplicitly] string PropertyName, string Message);

public abstract class Result
{
    protected Result(bool isSuccess, ValidationError[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; private set; }

    public ValidationError[] Errors { get; private set; }
}

public class Result<T> : Result
{
    private Result(bool isSuccess, T value, ValidationError[] errors) : base(isSuccess, errors)
    {
        Value = value;
    }

    public T Value { get; private set; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, Array.Empty<ValidationError>());
    }

    public static Result<T> Failure(ValidationError[] errors)
    {
        return new Result<T>(false, default!, errors);
    }

    public static Result<T> SingleFailure(string propertyName, string errorMessage)
    {
        return Failure(new[] {new ValidationError(propertyName, errorMessage)});
    }
}