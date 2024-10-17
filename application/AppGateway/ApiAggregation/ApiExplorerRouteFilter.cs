using Yarp.ReverseProxy.Configuration;

namespace PlatformPlatform.AppGateway.ApiAggregation;

public class ApiExplorerRouteFilter : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        return new ValueTask<ClusterConfig>(cluster);
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        if (route.RouteId == "openapi")
        {
            // Create a new RouteConfig with the updated ClusterId
            return new ValueTask<RouteConfig>(route with { ClusterId = "app-gateway" });
        }

        return new ValueTask<RouteConfig>(route);
    }
}
