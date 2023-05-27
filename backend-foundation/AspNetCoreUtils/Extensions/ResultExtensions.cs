using System.Net;
using System.Text.RegularExpressions;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace PlatformPlatform.Foundation.AspNetCoreUtils.Extensions;

public static class ResultExtensions
{
    public static IResult AsHttpResult<T, TDto>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value!.Adapt<TDto>())
            : GetProblemDetailsAsJson<T, TDto>(result);
    }

    public static IResult AsHttpResult<T, TDto>(this Result<T> result, string uri)
    {
        return result.IsSuccess
            ? Results.Created(uri, result.Value!.Adapt<TDto>())
            : GetProblemDetailsAsJson<T, TDto>(result);
    }

    private static IResult GetProblemDetailsAsJson<T, TDto>(Result<T> result)
    {
        return Results.Json(CreateProblemDetails(result),
            statusCode: (int) result.StatusCode);
    }

    private static ProblemDetails CreateProblemDetails<T>(Result<T> result)
    {
        if (result.Errors.Any())
        {
            return new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{(int) result.StatusCode}",
                Title = GetHttpStatusCodeTitle(result.StatusCode),
                Status = (int) result.StatusCode,
                Detail = result.ErrorMessage?.Message,
                Extensions = {{nameof(result.Errors), result.Errors}}
            };
        }

        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{(int) result.StatusCode}",
            Title = GetHttpStatusCodeTitle(result.StatusCode),
            Status = (int) result.StatusCode,
            Detail = result.ErrorMessage?.Message
        };
    }

    private static string GetHttpStatusCodeTitle(HttpStatusCode statusCode)
    {
        return Regex.Replace(statusCode.ToString(), "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
    }
}