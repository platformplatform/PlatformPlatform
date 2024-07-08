using Yarp.ReverseProxy.Configuration;

namespace PlatformPlatform.AppGateway.Filters;

public class ClusterDestinationConfigFilter : IProxyConfigFilter
{
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        return cluster.ClusterId switch
        {
            "account-management-api" => ReplaceDestinationAddress(cluster, "ACCOUNT_MANAGEMENT_API_URL"),
            "avatars-storage" => ReplaceDestinationAddress(cluster, "AVATARS_STORAGE_URL"),
            "back-office-api" => ReplaceDestinationAddress(cluster, "BACK_OFFICE_API_URL"),
            _ => throw new InvalidOperationException($"Unknown Cluster ID {cluster.ClusterId}")
        };
    }

    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        return new ValueTask<RouteConfig>(route);
    }

    private static ValueTask<ClusterConfig> ReplaceDestinationAddress(ClusterConfig cluster, string environmentVariable)
    {
        var destinationAddress = Environment.GetEnvironmentVariable(environmentVariable);
        if (destinationAddress is null) return new ValueTask<ClusterConfig>(cluster);

        // Each cluster has a dictionary with one and only one destination
        var destination = cluster.Destinations!.Single();

        // This is read-only, so we'll create a new one with our updates
        var newDestinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
        {
            { destination.Key, destination.Value with { Address = destinationAddress } }
        };

        return new ValueTask<ClusterConfig>(cluster with { Destinations = newDestinations });
    }
}
