using Account.Features.Tenants.Domain;
using SharedKernel.Authentication;
using SharedKernel.Domain;

namespace Account.Api.Middleware;

public sealed class FeatureFlagVersionMiddleware(ITenantRepository tenantRepository) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            var versionClaim = context.User.FindFirst("feature_flag_version")?.Value;

            if (tenantIdClaim is not null && long.TryParse(tenantIdClaim, out var tenantIdValue) &&
                versionClaim is not null && int.TryParse(versionClaim, out var jwtVersion))
            {
                var tenantId = new TenantId(tenantIdValue);
                var currentVersion = await tenantRepository.GetFeatureFlagVersionAsync(tenantId, context.RequestAborted);

                if (jwtVersion != currentVersion)
                {
                    context.Response.OnStarting(() =>
                        {
                            context.Response.Headers[AuthenticationTokenHttpKeys.RefreshAuthenticationTokensHeaderKey] = "true";
                            return Task.CompletedTask;
                        }
                    );
                }
            }
        }

        await next(context);
    }
}
