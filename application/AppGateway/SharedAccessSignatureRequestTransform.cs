using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using Yarp.ReverseProxy.Transforms;

namespace PlatformPlatform.AppGateway;

public class SharedAccessSignatureRequestTransform(
    [FromKeyedServices("account-management-storage")] IBlobStorage blobStorage
) : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.Path.StartsWithSegments("/avatars"))
        {
            var sharedAccessSignature = blobStorage.GetSharedAccessSignature("avatars", TimeSpan.FromMinutes(10));
            context.HttpContext.Request.QueryString = new QueryString(sharedAccessSignature);
        }

        return ValueTask.CompletedTask;
    }
}