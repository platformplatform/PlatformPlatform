using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.ExecutionContext;

public class HttpExecutionContext(IHttpContextAccessor httpContextAccessor) : IExecutionContext
{
    private IPAddress? _clientIpAddress;
    private UserInfo? _userInfo;

    public TenantId? TenantId => UserInfo.TenantId;

    public UserInfo UserInfo
    {
        get
        {
            if (_userInfo is not null)
            {
                return _userInfo;
            }

            var browserLocale = httpContextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name;

            return _userInfo = UserInfo.Create(httpContextAccessor.HttpContext?.User, browserLocale);
        }
    }

    public IPAddress ClientIpAddress
    {
        get
        {
            if (_clientIpAddress is not null)
            {
                return _clientIpAddress;
            }

            if (httpContextAccessor.HttpContext == null)
            {
                return _clientIpAddress = IPAddress.None;
            }

            var forwardedIps = httpContextAccessor.HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',');
            if (IPAddress.TryParse(forwardedIps.LastOrDefault(), out var clientIpAddress))
            {
                return _clientIpAddress = clientIpAddress;
            }

            return _clientIpAddress = IPAddress.None;
        }
    }
}
