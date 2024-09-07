using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.ExecutionContext;

public class HttpExecutionContext(IHttpContextAccessor httpContextAccessor) : IExecutionContext
{
    private TenantId? _tenantId;
    private UserInfo? _userInfo;

    public TenantId? TenantId
    {
        get
        {
            // The first time this property is accessed, _userInfo might be null, but when TenantId.TryParse() is called,
            // it will be set. So even if _tenantId is null, we know if this property has been accessed before.
            if (_userInfo is not null)
            {
                return _tenantId;
            }

            TenantId.TryParse(UserInfo.TenantId, out _tenantId);
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

            var defaultLocale = httpContextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name;

            return _userInfo = UserInfo.Create(httpContextAccessor.HttpContext?.User, defaultLocale ?? "en-US");
        }
    }
}
