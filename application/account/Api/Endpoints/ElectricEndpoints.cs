using SharedKernel.Endpoints;
using SharedKernel.ExecutionContext;

namespace Account.Api.Endpoints;

public sealed class ElectricEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/electric";

    private static readonly Dictionary<string, string> AllowedTables = new()
    {
        ["users"] = "tenant_id",
        ["tenants"] = "id",
        ["subscriptions"] = "tenant_id",
        ["sessions"] = "tenant_id"
    };

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Electric").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/v1/shape", async (HttpContext httpContext, IExecutionContext executionContext, IConfiguration configuration) =>
            await ElectricShapeProxy.ProxyShapeRequest(httpContext, executionContext, configuration, AllowedTables)
        ).ExcludeFromDescription();
    }
}
