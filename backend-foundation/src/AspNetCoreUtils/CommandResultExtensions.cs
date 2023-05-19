using Microsoft.AspNetCore.Http;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.Foundation.AspNetCoreUtils;

public static class CommandResultExtensions
{
    public static IResult AsHttpResult<T>(this CommandResult<T> result)
    {
        return Results.Json(result.IsSuccess ? result.Value : result.Errors, statusCode: (int) result.StatusCode);
    }

    public static IResult AsHttpResult<T>(this CommandResult<T> result, string uri)
    {
        return result.IsSuccess
            ? Results.Created(uri, result.Value)
            : Results.Json(result.Errors, statusCode: (int) result.StatusCode);
    }
}