using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiCore.ApiResults;

public partial class ApiResult : IResult
{
    private readonly ResultBase _result;
    private readonly string? _routePrefix;

    protected ApiResult(ResultBase result, string? routePrefix = null)
    {
        _result = result;
        _routePrefix = routePrefix;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        return _routePrefix == null
            ? ConvertResult().ExecuteAsync(httpContext)
            : ConvertResult(_routePrefix).ExecuteAsync(httpContext);
    }

    protected virtual IResult ConvertResult(string? routePrefix = null)
    {
        if (!_result.IsSuccess) return GetProblemDetailsAsJson();

        return routePrefix == null
            ? Results.Ok()
            : Results.Created($"{routePrefix}/{_result}", null);
    }

    protected IResult GetProblemDetailsAsJson()
    {
        return Results.Json(CreateProblemDetails(), statusCode: (int) _result.StatusCode,
            contentType: "application/problem+json");
    }

    private ProblemDetails CreateProblemDetails()
    {
        var statusCode = _result.StatusCode;
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int) _result.StatusCode}",
            Title = SplitCamelCaseTitle(statusCode.ToString()),
            Status = (int) _result.StatusCode
        };

        if (_result.ErrorMessage is not null) problemDetails.Detail = _result.ErrorMessage.Message;

        if (_result.Errors?.Length > 0) problemDetails.Extensions[nameof(_result.Errors)] = _result.Errors;

        return problemDetails;
    }

    private static string SplitCamelCaseTitle(string title)
    {
        return SplitCamelCase().Replace(title, " $1");
    }

    [GeneratedRegex("(?<=[a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex SplitCamelCase();

    public static implicit operator ApiResult(Result result)
    {
        return new ApiResult(result);
    }
}

public class ApiResult<T> : ApiResult
{
    private readonly Result<T> _result;

    public ApiResult(Result<T> result, string? routePrefix = null)
        : base(result, routePrefix)
    {
        _result = result;
    }

    protected override IResult ConvertResult(string? routePrefix = null)
    {
        if (!_result.IsSuccess) return GetProblemDetailsAsJson();

        return routePrefix == null
            ? Results.Ok(_result.Value!.Adapt<T>())
            : Results.Created($"{routePrefix}/{_result.Value}", null);
    }

    public static implicit operator ApiResult<T>(Result<T> result)
    {
        return new ApiResult<T>(result);
    }
}