using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiResults;

public static class ApiResultExtensions
{
    public static ApiResult AddResourceUri<T>(this Result<T> result, string routePrefix)
    {
        return new ApiResult<T>(result, routePrefix);
    }
}
