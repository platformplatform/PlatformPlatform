using System.Net;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.Foundation.AspNetCoreUtils.Extensions;

public static class CommandResultExtensions
{
    public static IResult AsHttpResult<T, TDto>(this QueryResult<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value!.Adapt<TDto>())
            : Results.Json(CreateProblemDetails<T, TDto>("Validation Error", result, HttpStatusCode.NotFound),
                statusCode: (int) HttpStatusCode.NotFound);
    }

    public static IResult AsHttpResult<T, TDto>(this CommandResult<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value!.Adapt<TDto>())
            : Results.Json(CreateProblemDetails<T, TDto>("Validation Error", result),
                statusCode: (int) result.StatusCode);
    }

    public static IResult AsHttpResult<T, TDto>(this CommandResult<T> result, string uri)
    {
        return result.IsSuccess
            ? Results.Created(uri, result.Value!.Adapt<TDto>())
            : Results.Json(CreateProblemDetails<T, TDto>("Validation Error", result),
                statusCode: (int) result.StatusCode);
    }

    private static ProblemDetails CreateProblemDetails<T, TDto>(string title, QueryResult<T> result,
        HttpStatusCode httpStatusCode)
    {
        return new ProblemDetails
        {
            Type = httpStatusCode.ToString(),
            Title = title,
            Status = (int) httpStatusCode,
            Detail = result.ErrorMessage?.Message
        };
    }

    private static ProblemDetails CreateProblemDetails<T, TDto>(string title, CommandResult<T> result)
    {
        if (result.Errors.Any())
        {
            return new ProblemDetails
            {
                Type = result.StatusCode.ToString(),
                Title = title,
                Status = (int) result.StatusCode,
                Detail = result.ErrorMessage?.Message,
                Extensions = {{nameof(result.Errors), result.Errors}}
            };
        }

        return new ProblemDetails
        {
            Type = result.StatusCode.ToString(),
            Title = title,
            Status = (int) result.StatusCode,
            Detail = result.ErrorMessage?.Message
        };
    }
}