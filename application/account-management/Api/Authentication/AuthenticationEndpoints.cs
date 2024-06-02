using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Api.Authentication;

public class AuthenticationEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/authentication";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Authentication");

        group.MapPost("/", Task<ApiResult> (HttpContext http) =>
            {
                var authenticationTokens = new { AccessToken = UserId.NewId(), RefreshToken = TimeProvider.System.GetUtcNow().Ticks };

                http.Response.Headers.Remove("X-Refresh-Token");
                http.Response.Headers.Append("X-Refresh-Token", authenticationTokens.RefreshToken.ToString());

                http.Response.Headers.Remove("X-Access-Token");
                http.Response.Headers.Append("X-Access-Token", authenticationTokens.AccessToken.ToString());

                return Task.FromResult<ApiResult>(Result.Success());
            }
        );
    }
}
