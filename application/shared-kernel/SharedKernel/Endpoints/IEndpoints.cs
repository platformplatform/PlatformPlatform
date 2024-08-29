using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.Endpoints;

public interface IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes);
}
