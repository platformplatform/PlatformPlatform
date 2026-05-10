using Microsoft.AspNetCore.Mvc;
using SharedKernel.Integrations.BlobStorage;

namespace Account.Api;

// Back-office Kestrel listens on its own port (BACK_OFFICE_KESTREL_PORT) and bypasses AppGateway, so
// the avatar/logo routes that AppGateway forwards on the user-facing host are not available here.
// Map equivalent endpoints scoped to the back-office host that stream blobs directly from the
// keyed account-storage IBlobStorageClient. This keeps account list/side-pane logos and owner
// avatars working when the back-office SPA is loaded over the dedicated Kestrel port.
public static class BackOfficeBlobProxy
{
    public static IEndpointRouteBuilder MapBackOfficeBlobProxy(this IEndpointRouteBuilder routes, string backOfficeHostname)
    {
        routes.MapGet("/avatars/{**path}", async ([FromRoute] string path, [FromKeyedServices("account-storage")] IBlobStorageClient blobStorageClient, HttpContext httpContext, CancellationToken cancellationToken)
            => await StreamBlobAsync(blobStorageClient, "avatars", path, httpContext, cancellationToken)
        ).RequireHost(backOfficeHostname).AllowAnonymous();

        routes.MapGet("/logos/{**path}", async ([FromRoute] string path, [FromKeyedServices("account-storage")] IBlobStorageClient blobStorageClient, HttpContext httpContext, CancellationToken cancellationToken)
            => await StreamBlobAsync(blobStorageClient, "logos", path, httpContext, cancellationToken)
        ).RequireHost(backOfficeHostname).AllowAnonymous();

        return routes;
    }

    private static async Task<IResult> StreamBlobAsync(IBlobStorageClient blobStorageClient, string containerName, string blobName, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var blob = await blobStorageClient.DownloadAsync(containerName, blobName, cancellationToken);
        if (blob is null) return Results.NotFound();

        httpContext.Response.Headers.CacheControl = "public, max-age=2592000, immutable";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.Stream(blob.Value.Stream, blob.Value.ContentType);
    }
}
