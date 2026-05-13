using Yarp.ReverseProxy.Transforms;

namespace AppGateway.Transformations;

public class BlockInternalApiTransform : RequestTransform
{
    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.Request.Path.Value?.Contains("/internal-api/", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.HttpContext.Response.ContentType = "text/plain";
            await context.HttpContext.Response.WriteAsync("Access to internal API is forbidden.");
            // Finalize the response so the YARP forwarder cannot pick up the request and forward it
            // upstream after we've already written the 403 body.
            await context.HttpContext.Response.CompleteAsync();
        }
    }
}
