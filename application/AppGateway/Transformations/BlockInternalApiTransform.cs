using Yarp.ReverseProxy.Transforms;

namespace PlatformPlatform.AppGateway.Transformations;

public class BlockInternalApiTransform : RequestTransform
{
    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.Request.Path.Value?.Contains("/internal-api/", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.HttpContext.Response.ContentType = "text/plain";
            await context.HttpContext.Response.WriteAsync("Access to internal API is forbidden.");
        }
    }
}
