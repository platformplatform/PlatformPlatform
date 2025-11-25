using PlatformPlatform.SharedKernel.Integrations.BlobStorage;
using Yarp.ReverseProxy.Transforms;

namespace PlatformPlatform.AppGateway.Transformations;

public class SharedAccessSignatureRequestTransform([FromKeyedServices("account-management-storage")] IBlobStorageClient accountManagementBlobStorageClient)
    : RequestTransform
{
    public override ValueTask ApplyAsync(RequestTransformContext context)
    {
        string containerName;
        if (context.Path.StartsWithSegments("/avatars"))
        {
            containerName = "avatars";
        }
        else if (context.Path.StartsWithSegments("/logos"))
        {
            containerName = "logos";
        }
        else
        {
            return ValueTask.CompletedTask;
        }

        var sharedAccessSignature = accountManagementBlobStorageClient.GetSharedAccessSignature(containerName, TimeSpan.FromMinutes(10));
        context.HttpContext.Request.QueryString = new QueryString(sharedAccessSignature);

        return ValueTask.CompletedTask;
    }
}
