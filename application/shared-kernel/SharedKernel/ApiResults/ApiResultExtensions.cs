using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiResults;

public static class ApiResultExtensions
{
    public static ApiResult AddResourceUri<T>(this Result<T> result, string routePrefix)
    {
        return new ApiResult<T>(result, routePrefix);
    }

    public static ApiResult AddRefreshAuthenticationTokens(this Result result)
    {
        if (!result.IsSuccess) return new ApiResult(result);

        return new ApiResult(result, httpHeaders: new Dictionary<string, string>
            { { AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey, "true" } }
        );
    }
}
