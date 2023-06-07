using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace PlatformPlatform.SharedKernel.ApiCore.HttpResults;

public partial class ApiResult : IResult
{
    private readonly Result _result;
    private readonly string? _routePrefix;

    public ApiResult(Result result, string? routePrefix = null)
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

    private IResult ConvertResult(string? routePrefix = null)
    {
        if (!_result.IsSuccess) return GetProblemDetailsAsJson();

        return routePrefix == null
            ? Results.Ok()
            : Results.Created($"{routePrefix}/{_result}", null);
    }

    private IResult GetProblemDetailsAsJson()
    {
        return Results.Json(CreateProblemDetails(), statusCode: (int) _result.StatusCode);
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

public partial class ApiResult<T> : IResult
{
    private readonly Result<T> _result;
    private readonly string? _routePrefix;

    public ApiResult(Result<T> result, string? routePrefix = null)
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

    private IResult ConvertResult(string? routePrefix = null)
    {
        if (!_result.IsSuccess) return GetProblemDetailsAsJson();

        return routePrefix == null
            ? Results.Ok(_result.Value!.Adapt<T>())
            : Results.Created($"{routePrefix}/{_result.Value}", null);
    }

    private IResult GetProblemDetailsAsJson()
    {
        return Results.Json(CreateProblemDetails(), statusCode: (int) _result.StatusCode);
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

    public static implicit operator ApiResult<T>(Result<T> result)
    {
        return new ApiResult<T>(result);
    }
}