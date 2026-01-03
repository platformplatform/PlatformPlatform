namespace PlatformPlatform.SharedKernel.EntityFramework;

/// <summary>
///     Contains the names of EF Core named query filters used throughout the application.
///     Named filters allow selective disabling at query time using IgnoreQueryFilters(["FilterName"]).
/// </summary>
public static class QueryFilterNames
{
    /// <summary>
    ///     Filter that restricts queries to entities belonging to the current tenant.
    ///     Applied to all entities implementing <see cref="Domain.ITenantScopedEntity" />.
    /// </summary>
    public const string Tenant = "Tenant";

    /// <summary>
    ///     Filter that excludes soft-deleted entities from queries.
    ///     Applied to all entities implementing <see cref="Domain.ISoftDeletable" />.
    /// </summary>
    public const string SoftDelete = "SoftDelete";
}
