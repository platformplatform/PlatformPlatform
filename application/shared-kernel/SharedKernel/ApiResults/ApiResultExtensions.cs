using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.SharedKernel.ApiResults;

public static class ApiResultExtensions
{
    extension(Result result)
    {
        public ApiResult AddRefreshAuthenticationTokens()
        {
            if (!result.IsSuccess) return new ApiResult(result);

            return new ApiResult(result, httpHeaders: new Dictionary<string, string>
                { { AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey, "true" } }
            );
        }
    }
}
