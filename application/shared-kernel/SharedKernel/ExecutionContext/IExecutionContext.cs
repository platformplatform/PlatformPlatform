using System.Net;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.ExecutionContext;

/// <summary>
///     Represents the execution context of the current operation, providing information
///     about the tenant and the authenticated user making the request.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    ///     Gets the current tenant identifier. May be null if the operation is not tenant-scoped.
    /// </summary>
    TenantId? TenantId { get; }

    /// <summary>
    ///     Gets information about the current user making the request, including authentication status,
    ///     user ID, and other identity-related details.
    ///     If the user is not authenticated, it will be set to <see cref="Authentication.UserInfo.System" />.
    /// </summary>
    UserInfo UserInfo { get; }

    IPAddress ClientIpAddress { get; }
}
