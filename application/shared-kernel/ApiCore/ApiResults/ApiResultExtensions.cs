using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiCore.ApiResults;

public static class ApiResultExtensions
{
    public static ApiResult AddResourceUri<T>(this Result<T> result, string routePrefix)
    {
        return new ApiResult<T>(result, routePrefix);
    }
}