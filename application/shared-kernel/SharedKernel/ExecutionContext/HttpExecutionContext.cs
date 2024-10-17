using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.ExecutionContext;

public class HttpExecutionContext(IHttpContextAccessor httpContextAccessor) : IExecutionContext
{
    private bool _isTenantIdCalculated;
    private TenantId? _tenantId;
    private UserInfo? _userInfo;

    public TenantId? TenantId
    {
        get
        {
            if (_isTenantIdCalculated)
            {
                return _tenantId;
            }

            TenantId.TryParse(UserInfo.TenantId, out _tenantId);
            _isTenantIdCalculated = true;
            return _tenantId;
        }
    }

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
}
