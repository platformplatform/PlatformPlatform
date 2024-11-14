using System.Net;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.ExecutionContext;

public class BackgroundWorkerExecutionContext(TenantId? tenantId = null, UserInfo? userInfo = null)
    : IExecutionContext
{
    public TenantId? TenantId { get; } = tenantId;

    public UserInfo UserInfo { get; } = userInfo ?? UserInfo.System;

    public IPAddress ClientIpAddress { get; } = IPAddress.None;
}
