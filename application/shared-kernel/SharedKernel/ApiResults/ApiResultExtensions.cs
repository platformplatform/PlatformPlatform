using SharedKernel.Authentication;
using SharedKernel.Cqrs;

namespace SharedKernel.ApiResults;

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

    extension<T>(Result<T> result)
    {
        public ApiResult<T> AddRefreshAuthenticationTokens()
        {
            if (!result.IsSuccess) return new ApiResult<T>(result);

            return new ApiResult<T>(result, httpHeaders: new Dictionary<string, string>
                { { AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey, "true" } }
            );
        }
    }
}
