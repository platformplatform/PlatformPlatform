using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiCore.ApiResults;

public partial class ApiResult(ResultBase result, string? routePrefix = null) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return routePrefix == null
            ? ConvertResult().ExecuteAsync(httpContext)
            : ConvertResult(routePrefix).ExecuteAsync(httpContext);
    }

    protected virtual IResult ConvertResult(string? routePrefix = null)
    {
        if (!result.IsSuccess) return GetProblemDetailsAsJson();

        return routePrefix == null
            ? Results.Ok()
            : Results.Created($"{routePrefix}/{result}", null);
    }

    protected IResult GetProblemDetailsAsJson()
    {
        return Results.Json(CreateProblemDetails(), statusCode: (int)result.StatusCode,
            contentType: "application/problem+json");
    }

    private ProblemDetails CreateProblemDetails()
    {
        var statusCode = result.StatusCode;
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int)result.StatusCode}",
            Title = SplitCamelCaseTitle(statusCode.ToString()),
            Status = (int)result.StatusCode
        };

        if (result.ErrorMessage is not null) problemDetails.Detail = result.ErrorMessage.Message;

        if (result.Errors?.Length > 0) problemDetails.Extensions[nameof(result.Errors)] = result.Errors;

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

public class ApiResult<T>(Result<T> result, string? routePrefix = null) : ApiResult(result, routePrefix)
{
    protected override IResult ConvertResult(string? routePrefix = null)
    {
        if (!result.IsSuccess) return GetProblemDetailsAsJson();

        return routePrefix == null
            ? Results.Ok(result.Value!.Adapt<T>())
            : Results.Created($"{routePrefix}/{result.Value}", null);
    }

    public static implicit operator ApiResult<T>(Result<T> result)
    {
        return new ApiResult<T>(result);
    }
}