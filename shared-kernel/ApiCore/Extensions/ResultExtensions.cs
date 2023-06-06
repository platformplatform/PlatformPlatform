using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.Entities;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace PlatformPlatform.SharedKernel.ApiCore.Extensions;

public static partial class ResultExtensions
{
    public static IResult AsHttpResult<T>(this Result<T> result)
        where T : IIdentity
    {
        return result.IsSuccess
            ? Results.Ok(result.Value!.Adapt<T>())
            : GetProblemDetailsAsJson(result);
    }

    public static IResult AsHttpResult<T>(this Result<T> result, string routePrefix)
        where T : IIdentity
    {
        return result.IsSuccess
            ? Results.Created($"{routePrefix}/{result.Value!.GetId()}", null)
            : GetProblemDetailsAsJson(result);
    }

    private static IResult GetProblemDetailsAsJson<T>(Result<T> result)
    {
        return Results.Json(CreateProblemDetails(result), statusCode: (int) result.StatusCode);
    }

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