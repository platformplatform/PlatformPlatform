using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.SharedKernel.DomainModeling.Cqrs;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace PlatformPlatform.SharedKernel.ApiCore.Extensions;

public static partial class ResultExtensions
{
    public static IResult AsHttpResult<T, TDto>(this Result<T> result)
    {
        return result.IsSuccess ? Results.Ok(result.Value!.Adapt<TDto>()) : GetProblemDetailsAsJson(result);
    }

    public static IResult AsHttpResult<T, TDto>(this Result<T> result, string uri)
    {
        return result.IsSuccess ? Results.Created(uri, result.Value!.Adapt<TDto>()) : GetProblemDetailsAsJson(result);
    }

    private static IResult GetProblemDetailsAsJson<T>(Result<T> result)
    {
        return Results.Json(CreateProblemDetails(result), statusCode: (int) result.StatusCode);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static ProblemDetails CreateProblemDetails<T>(Result<T> result)
    {
        var statusCode = result.StatusCode;
        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int) result.StatusCode}",
            Title = SplitCamelCaseTitle(statusCode.ToString()),
            Status = (int) result.StatusCode
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
}