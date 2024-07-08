using Azure.Core;
using Yarp.ReverseProxy.Transforms;

namespace PlatformPlatform.AppGateway.Transformations;

public class ManagedIdentityTransform(TokenCredential credential)
    : RequestHeaderTransform("Authorization", false)
{
    protected override string? GetValue(RequestTransformContext context)
    {
        if (!context.HttpContext.Request.Path.StartsWithSegments("/avatars", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var tokenRequestContext = new TokenRequestContext(["https://storage.azure.com/.default"]);
        var token = credential.GetToken(tokenRequestContext, context.HttpContext.RequestAborted);
        return $"Bearer {token.Token}";
    }
}

public class ApiVersionHeaderTransform() : RequestHeaderTransform("x-ms-version", false)
{
    protected override string? GetValue(RequestTransformContext context)
    {
        return !context.HttpContext.Request.Path.StartsWithSegments("/avatars") ? null : "2023-11-03";
    }
}
