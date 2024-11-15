using PlatformPlatform.SharedKernel.Integrations.BlobStorage;
using Yarp.ReverseProxy.Transforms;

namespace PlatformPlatform.AppGateway.Transformations;

public class SharedAccessSignatureRequestTransform([FromKeyedServices("avatars-storage")] BlobStorageClient blobStorageClient)
    : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (!context.Path.StartsWithSegments("/avatars")) return ValueTask.CompletedTask;

        var sharedAccessSignature = blobStorageClient.GetSharedAccessSignature("avatars", TimeSpan.FromMinutes(10));
        context.HttpContext.Request.QueryString = new QueryString(sharedAccessSignature);

        return ValueTask.CompletedTask;
    }
}
