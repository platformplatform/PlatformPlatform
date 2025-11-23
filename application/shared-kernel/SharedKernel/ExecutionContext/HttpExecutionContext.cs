using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.ExecutionContext;

public class HttpExecutionContext(IHttpContextAccessor httpContextAccessor) : IExecutionContext
{
    public TenantId? TenantId => UserInfo.TenantId;

    public UserInfo UserInfo
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            var browserLocale = httpContextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name;

            return field = UserInfo.Create(httpContextAccessor.HttpContext?.User, browserLocale);
        }
    }

    public IPAddress ClientIpAddress
    {
        get
        {
            if (field is not null)
            {
                return field;
            }

            if (httpContextAccessor.HttpContext == null)
            {
                return field = IPAddress.None;
            }

            var forwardedIps = httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',');
            if (IPAddress.TryParse(forwardedIps.LastOrDefault(), out var clientIpAddress))
            {
                return field = clientIpAddress;
            }

            return field = IPAddress.None;
        }
    }
}
