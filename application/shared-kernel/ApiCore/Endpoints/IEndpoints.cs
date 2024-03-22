using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.ApiCore.Endpoints;

public interface IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes);
}