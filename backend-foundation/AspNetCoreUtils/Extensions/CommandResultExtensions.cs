using Mapster;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.Foundation.AspNetCoreUtils.Extensions;

public static class CommandResultExtensions
{
    public static IResult AsHttpResult<T, TDto>(this QueryResult<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value!.Adapt<TDto>())
            : Results.NotFound(result.ErrorMessage);
    }

    public static IResult AsHttpResult<T, TDto>(this CommandResult<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value!.Adapt<TDto>());
        }

        return result.Errors.Length > 0
            ? Results.Json(result.Errors, statusCode: (int) result.StatusCode)
            : Results.Json(result.ErrorMessage, statusCode: (int) result.StatusCode);
    }

    public static IResult AsHttpResult<T, TDto>(this CommandResult<T> result, string uri)
    {
        return result.IsSuccess
            ? Results.Created(uri, result.Value!.Adapt<TDto>())
            : Results.Json(result.Errors, statusCode: (int) result.StatusCode);
    }
}