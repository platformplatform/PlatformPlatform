using Microsoft.AspNetCore.Routing;

namespace SharedKernel.Endpoints;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IEndpoints
{
    public void MapEndpoints(IEndpointRouteBuilder routes);
}
