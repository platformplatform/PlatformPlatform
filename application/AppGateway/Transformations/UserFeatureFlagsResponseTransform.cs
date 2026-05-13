using Microsoft.IdentityModel.JsonWebTokens;
using SharedKernel.Authentication;
using Yarp.ReverseProxy.Transforms;

namespace AppGateway.Transformations;

/// <summary>
///     Emits the `x-user-feature-flags` response header on every authenticated YARP response, with
///     the comma-separated feature-flag keys from the (post-refresh) access-token claim. The SPA
///     diffs the value against its current state and updates only on change.
///     Implemented as a YARP `ResponseTransform` rather than an `OnStarting` callback because
///     `OnStarting` was empirically unreliable for the YARP-proxied path - the callback did not
///     fire in time to mutate response headers before they were flushed to the client.
///     `ResponseTransform.ApplyAsync` runs after the upstream response is received and before
///     YARP calls `Response.StartAsync`, which is a guaranteed mutation point.
/// </summary>
public sealed class UserFeatureFlagsResponseTransform : ResponseTransform
{
    public const string CurrentAccessTokenItemKey = "CurrentAccessToken";

    private static readonly JsonWebTokenHandler TokenHandler = new();

    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (context.HttpContext.Items.TryGetValue(CurrentAccessTokenItemKey, out var tokenItem) && tokenItem is string accessToken)
        {
            context.HttpContext.Response.Headers[AuthenticationTokenHttpKeys.UserFeatureFlagsHeaderKey] = ExtractFeatureFlagsClaim(accessToken);
        }

        return ValueTask.CompletedTask;
    }

    private static string ExtractFeatureFlagsClaim(string accessToken)
    {
        if (!TokenHandler.CanReadToken(accessToken)) return string.Empty;
        var jwt = TokenHandler.ReadJsonWebToken(accessToken);
        return jwt.TryGetClaim("feature_flags", out var claim) ? claim.Value : string.Empty;
    }
}
