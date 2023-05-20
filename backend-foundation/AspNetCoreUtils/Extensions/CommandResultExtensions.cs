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
            : Results.NotFound(result.Error);
    }

    public static IResult AsHttpResult<T, TDto>(this CommandResult<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value!.Adapt<TDto>())
            : Results.Json(result.Errors, statusCode: (int) result.StatusCode);
    }

    public static IResult AsHttpResult<T, TDto>(this CommandResult<T> result, string uri)
    {
        return result.IsSuccess
            ? Results.Created(uri, result.Value!.Adapt<TDto>())
            : Results.Json(result.Errors, statusCode: (int) result.StatusCode);
    }
}