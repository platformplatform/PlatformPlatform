using System.Security.Claims;
using Account.Features.SupportTickets.Domain;
using Microsoft.AspNetCore.Http;

namespace Account.Features.SupportTickets.Shared;

// Resolves the current back-office staff identity from HttpContext.User. Back-office principals
// (Easy Auth / mock easy auth) carry no UserId, TenantId, or SessionId — IExecutionContext.UserInfo
// is shaped for tenant-scoped account users and would lose the BackOfficeIdentity claim shape we
// need to record on tickets. ObjectId is the Entra oid (ClaimTypes.NameIdentifier); DisplayName is
// the friendly OIDC `name` claim.
public sealed class BackOfficeStaffContext(IHttpContextAccessor httpContextAccessor)
{
    public BackOfficeStaffRef GetCurrent()
    {
        var user = httpContextAccessor.HttpContext?.User ?? throw new InvalidOperationException("No active HttpContext for back-office staff resolution.");
        var objectId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var displayName = user.FindFirstValue(ClaimTypes.Name) ?? objectId;
        return new BackOfficeStaffRef(objectId, displayName);
    }
}
