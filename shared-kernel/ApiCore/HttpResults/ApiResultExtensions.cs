using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.SharedKernel.ApiCore.HttpResults;

public static class ApiResultExtensions
{
    public static ApiResult<T> AddResourceUri<T>(this Result<T> result, string routePrefix)
        where T : IIdentity
    {
        return new ApiResult<T>(result, routePrefix);
    }
}