---
paths: **/*Repository.cs
description: Rules for DDD repositories, including tenant scoping, interface conventions, and use of Entity Framework
---

# DDD Repositories

Guidelines for implementing DDD repositories in the backend, including structure, interface conventions, and Entity Framework mapping.

## Implementation

1. Create repositories alongside their corresponding aggregates in `/[scs-name]/Core/Features/[Feature]/Domain`
2. Create a public sealed class implementation using a primary constructor
3. All implementations must inherit from `RepositoryBase<TAggregate, TId>`
4. Create an interface that extends `IBaseRepository<TAggregate, TId>` or `ICrudRepository<TAggregate, TId>`:
   - Use `IBaseRepository` when you don't need all CRUD operations
   - Only include methods needed for your specific aggregate
5. Only return Aggregates or custom projections—never Entities or Value Objects
6. Never return `[PublicAPI]` response DTOs (repositories return domain objects; mapping to DTOs happens in query handlers)
7. Keep repositories focused on persistence operations, not business logic
8. Repositories are automatically registered in the DI container
9. Aggregates with `ITenantScopedEntity` are automatically filtered by tenant using EF Core query filters:
   - In rare cases, disable this using `IgnoreQueryFilters` (e.g., looking up anonymous user by email)
   - Always name the filter via `QueryFilterNames` (e.g., `.IgnoreQueryFilters([QueryFilterNames.Tenant])`)—never the parameterless overload
   - If using `IgnoreQueryFilters`, add an `UnfilteredAsync` suffix and an XML comment warning about disabled filters
10. Use `IEntityTypeConfiguration<TAggregate>` for EF Core model configuration
11. Map strongly typed IDs in EF Core configurations using:
    - `MapStronglyTypedUuid` for ULIDs
    - `MapStronglyTypedLongId` for long IDs
    - `MapStronglyTypedGuid` for GUIDs
12. Updating entities doesn't belong in repositories—fetch the aggregate in commands, update it, then save via the repository
13. Never add `.AsTracking()` to queries—use `repository.Update()` which handles tracking internally
14. Never do N+1 queries
15. Don't register repositories in DI—SharedKernel registers them automatically
16. Don't add DbSets to DbContext—RepositoryBase handles this automatically
17. Add `.OrderBy(e => e.Id)` when caller depends on order (`[0]`, `.First()`, pagination)—ULIDs are chronological

## Examples

```csharp
// ✅ DO: Only include needed methods, use correct base interface, and inherit RepositoryBase
public interface ILoginRepository : IAppendRepository<Login, LoginId> // ✅ DO: Use only needed base interface
{
    void Update(Login aggregate); // ✅ DO: Add only needed methods
}

public sealed class LoginRepository(AccountDbContext accountDbContext) // ✅ DO: Use sealed class and primary constructor
    : RepositoryBase<Login, LoginId>(accountDbContext), ILoginRepository;

// ❌ DON'T: Use ICrudRepository if not all CRUD ops needed, or return DTOs
internal interface IBadLoginRepository : ICrudRepository<Login, LoginId> // ❌ DON'T: Make repositories internal
{
    Task<LoginDto> GetDto(LoginId id); // ❌ DON'T: Return DTOs from repositories, map entities in the query
}

// ✅ DO: Example with a custom query method
public interface IEmailConfirmationRepository : IAppendRepository<EmailConfirmation, EmailConfirmationId>
{
    EmailConfirmation[] GetByEmail(string email); // ✅ DO: Custom query method allowed
}

public sealed class EmailConfirmationRepository(AccountDbContext accountDbContext) // ✅ DO: Use sealed class and inherit RepositoryBase
    : RepositoryBase<EmailConfirmation, EmailConfirmationId>(accountDbContext), IEmailConfirmationRepository
{
    public EmailConfirmation[] GetByEmail(string email)
        => DbSet.Where(ec => !ec.Completed && ec.Email == email.ToLowerInvariant()).ToArray(); // ✅ DO: Implement custom query
}

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options, IExecutionContext executionContext)
    : SharedKernelDbContext<AccountDbContext>(options, executionContext)
{
    public DbSet<EmailConfirmation> EmailConfirmations => Set<EmailConfirmation>(); // ❌ DON'T: Add DbSet<T> to DbContext, this is automatically handled in RepositoryBase
}

```

### Use of IgnoreQueryFilters

Always pass a named filter via `QueryFilterNames`. The method must have an `UnfilteredAsync` suffix and an XML comment warning that the filter is disabled.

```csharp
/// <summary> // ✅ DO: XML comment explaining why
///     Retrieves a user by email without applying the tenant query filter.
/// </summary>
public async Task<User?> GetUserByEmailUnfilteredAsync(string email, CancellationToken cancellationToken) // ✅ DO: UnfilteredAsync suffix
{
    return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken); // ✅ DO: Named filter
}

// ❌ DON'T: Parameterless overload—disables every filter at once
return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(...);

// ❌ DON'T: Missing UnfilteredAsync suffix and XML comment
public async Task<User?> GetUserByEmail(...) { ... }
```
