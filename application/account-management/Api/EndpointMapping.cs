namespace PlatformPlatform.AccountManagement.Api;

public static class EndpointMapping
{
    public static void MapEndpoints<T>(this IEndpointRouteBuilder routes) where T : IEndpoints, new()
    {
        var endpointRegistration = new T();
        endpointRegistration.MapEndpoints(routes);
    }
}

public interface IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes);
}