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
        ["tenants"] = new ShapeTableConfig("id", ["id", "created_at", "modified_at", "name", "state", "suspension_reason", "logo", "plan"]),
        ["subscriptions"] = new ShapeTableConfig("tenant_id", ["id", "created_at", "modified_at", "plan", "scheduled_plan", "cancel_at_period_end", "first_payment_failed_at", "cancellation_reason", "cancellation_feedback", "current_price_amount", "current_price_currency", "current_period_end", "payment_transactions", "payment_method", "billing_info"], nameof(UserRole.Owner)),
        ["feature_flags"] = new ShapeTableConfig("tenant_id", ["id", "flag_key", "tenant_id", "user_id", "enabled_at", "disabled_at", "bucket_start", "bucket_end", "configurable_by_tenant", "configurable_by_user"], UserScopedColumn: "user_id", IncludeNullRows: true)
    };

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Electric").RequireAuthorization().ProducesValidationProblem();

        group.MapGet("/v1/shape", async (HttpContext httpContext, IExecutionContext executionContext, IConfiguration configuration) =>
            await ElectricShapeProxy.ProxyShapeRequest(httpContext, executionContext, configuration, AllowedTables)
        ).ExcludeFromDescription().DisableRequestTimeout();
    }
}
