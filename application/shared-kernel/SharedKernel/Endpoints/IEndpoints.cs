using Microsoft.AspNetCore.Routing;

namespace PlatformPlatform.SharedKernel.Endpoints;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes);
}
