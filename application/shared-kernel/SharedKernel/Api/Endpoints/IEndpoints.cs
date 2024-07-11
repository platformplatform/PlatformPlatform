using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.Api.Endpoints;

public interface IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes);
}
