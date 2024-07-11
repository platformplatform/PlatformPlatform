using PlatformPlatform.SharedKernel.Application.Cqrs;

namespace PlatformPlatform.SharedKernel.Api.ApiResults;

public static class ApiResultExtensions
{
    public static ApiResult AddResourceUri<T>(this Result<T> result, string routePrefix)
    {
        return new ApiResult<T>(result, routePrefix);
    }
}
