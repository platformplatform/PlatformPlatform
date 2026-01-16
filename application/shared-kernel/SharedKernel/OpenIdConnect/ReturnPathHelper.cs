using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.OpenIdConnect;

public static class ReturnPathHelper
{
    public const string ReturnPathCookieName = "__Host_Return_Path";

    public static void SetReturnPathCookie(HttpContext httpContext, string returnPath)
    {
        if (!IsValidRelativePath(returnPath))
        {
            return;
        }

        httpContext.Response.Cookies.Append(
            ReturnPathCookieName,
            returnPath,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true
            }
        );
    }

    public static string? GetReturnPathCookie(HttpContext httpContext)
    {
        var returnPath = httpContext.Request.Cookies[ReturnPathCookieName];

        if (string.IsNullOrEmpty(returnPath))
        {
            return null;
        }

        if (!IsValidRelativePath(returnPath))
        {
            return null;
        }

        return returnPath;
    }

    public static void ClearReturnPathCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(ReturnPathCookieName);
    }

    private static bool IsValidRelativePath(string path)
    {
        if (!path.StartsWith('/'))
        {
            return false;
        }

        if (path.StartsWith("//"))
        {
            return false;
        }

        if (path.Contains('\\'))
        {
            return false;
        }

        if (path.Contains("://"))
        {
            return false;
        }

        return true;
    }
}
