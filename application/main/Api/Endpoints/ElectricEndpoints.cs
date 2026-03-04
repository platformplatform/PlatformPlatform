using SharedKernel.Endpoints;
using SharedKernel.ExecutionContext;

namespace Main.Api.Endpoints;

public sealed class ElectricEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/main/electric";

    // ReSharper disable once CollectionNeverUpdated.Local -- Tables will be added when main SCS gets syncable entities
    private static readonly Dictionary<string, ShapeTableConfig> AllowedTables = new();

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Electric").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/v1/shape", async (HttpContext httpContext, IExecutionContext executionContext, IConfiguration configuration) =>
            await ElectricShapeProxy.ProxyShapeRequest(httpContext, executionContext, configuration, AllowedTables)
        ).ExcludeFromDescription();
    }
}
