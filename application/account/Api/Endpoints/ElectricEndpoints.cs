using Account.Features.Users.Domain;
using SharedKernel.Endpoints;
using SharedKernel.ExecutionContext;

namespace Account.Api.Endpoints;

public sealed class ElectricEndpoints : IEndpoints
{
    private const string RoutesPrefix = "/api/account/electric";

    private static readonly Dictionary<string, ShapeTableConfig> AllowedTables = new()
    {
        ["users"] = new ShapeTableConfig("tenant_id", ["id", "created_at", "modified_at", "email", "first_name", "last_name", "title", "role", "email_confirmed", "avatar", "locale", "last_seen_at", "deleted_at"]),
        ["tenants"] = new ShapeTableConfig("id", ["id", "created_at", "modified_at", "name", "state", "suspension_reason", "logo", "plan", "contact_info"]),
        ["subscriptions"] = new ShapeTableConfig("tenant_id", ["id", "created_at", "modified_at", "plan", "scheduled_plan", "cancel_at_period_end", "first_payment_failed_at", "cancellation_reason", "cancellation_feedback", "current_price_amount", "current_price_currency", "current_period_end", "payment_transactions", "payment_method", "billing_info"], nameof(UserRole.Owner))
    };

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Electric").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/v1/shape", async (HttpContext httpContext, IExecutionContext executionContext, IConfiguration configuration) =>
            await ElectricShapeProxy.ProxyShapeRequest(httpContext, executionContext, configuration, AllowedTables)
        ).ExcludeFromDescription().DisableRequestTimeout();
    }
}
