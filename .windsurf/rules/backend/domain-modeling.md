---
trigger: glob
globs: **/Domain/*.cs
description: Rules for creating DDD aggregates, entities, value objects, and Entity Framework configuration
---

# Domain Modeling

Carefully follow these instructions when implementing DDD models for aggregates, entities, and value objects.

## Implementation

1. Create all DDD models in the `/[scs-name]/Core/Features/[Feature]/Domain` directory, including aggregates, entities, value objects, strongly typed IDs, repositories, and Entity Framework Core mapping.
2. Understand the core DDD concepts:
   - Aggregates are the root of the DDD model and map 1:1 to database tables.
   - Entities belong to aggregates but have their own identity.
   - Value objects are immutable and have no identity.
   - Repositories are used to add, get, update, and remove aggregates; consult [Repositories](/.windsurf/rules/backend/repositories.md) for details.
3. Store entities and value objects as JSON columns on the Aggregate in the database to avoid EF Core's `.Include()` method.
4. For Aggregates:
   - Use public sealed classes that inherit from `AggregateRoot<TId>`.
   - Create a strongly typed ID for aggregates; consult [Strongly Typed IDs](/.windsurf/rules/backend/strongly-typed-ids.md) for details.
   - Never use navigational properties to other aggregates (e.g., don't use `User.Tenant`, or `Order.Customer`).
   - Use factory methods when creating aggregates.
   - Make properties private, and use methods when changing state and enforcing business rules.
   - Make properties immutable.
   - For many-to-many aggregates, make them Tenant scoped (ITenantScopedEntity) to include TenantId column, even if foreign key constraints already ensure tenant isolation.
   - For many-to-many aggregates, carefully consider if cascade delete is needed. If so, use `OnDelete(DeleteBehavior.Cascade)` in the EF Core IEntityTypeConfiguration.
5. For Entities:
   - Use public sealed classes that inherit from `Entity<TId>`.
   - Create a strongly typed ID for entities.
   - Use factory methods when creating entities.
   - Use private setters to control state changes.
   - Make properties private, and use methods when changing state and enforcing business rules.
   - Store entities as JSON columns on the Aggregate in the database.
6. For Value Objects:
   - Use records to ensure immutability.
   - Value objects do not have an ID.
7. Do NOT add Entity Framework configuration for primitive properties:
   - We do not use Entity Framework tooling for creating migrations, so there is no need for primitive property configuration, like length of fields or nullable properties.
   - Only configure Entity Framework properties for complex types, like collections, and value objects, that Entity Framework uses for generating SQL statements.
8. When implementing a new aggregate, start with the minimum required methods for the current feature. Add additional methods as new features require them, ensuring each method maintains aggregate invariants.

## Examples

```csharp
// Invoice.cs
public sealed class Invoice : AggregateRoot<InvoiceId>, ITenantScopedEntity // ✅ DO: Make aggregates tenant scoped by default
{
    private Invoice(TenantId tenantId, Address address)
        : base(InvoiceId.NewId())
    {
        TenantId = tenantId;
        Address = address;
        Status = InvoiceStatus.Created;
        InvoiceLines = ImmutableArray<InvoiceLine>.Empty;
    }

    public InvoiceStatus Status { get; private set; }

    public Address Address { get; private set; }

    public ImmutableArray<InvoiceLine> InvoiceLines { get; private set; } // ✅ DO: Use ImmutableArray as default collection type

    public TenantId TenantId { get; }

    public static Invoice Create(TenantId tenantId, Address address) // ✅ DO: Use factory methods
    {
        return new Invoice(tenantId, address);
    }

    public void SetStatus(InvoiceStatus status)// ✅ DO: Use methods for mutations
    {
        Status = status;
    }

    public void AddInvoiceLine(string description, decimal price)
    {
        var invoiceLine = InvoiceLine.Create(description, price);
        InvoiceLines = InvoiceLines.Add(invoiceLine);
    }
}

[PublicAPI]
[IdPrefix("order")] // ✅ DO: Create strongly typed prefix with max 5 characters
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, InvoiceId>))]
public sealed record InvoiceId(string Value) : StronglyTypedUlid<InvoiceId>(Value);

public sealed record Address(string Street, string City, string State, string ZipCode);

// InvoiceLine.cs   
public sealed class InvoiceLine : Entity<InvoiceLineId>
{
    private InvoiceLine(string description, decimal price)
        : base(InvoiceLineId.NewId())
    {
        Description = description;
        UnitPrice = price;
    }

    public string Description { get; init; } // ✅ DO: Use init for properties that cannot be changed

    public decimal Price { get; init; }

    internal static InvoiceLine Create(string description, decimal price)
    {
        return new InvoiceLine(description, price);
    }
}

[PublicAPI]
[IdPrefix("invln")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, InvoiceLineId>))]
public sealed record InvoiceLineId(string Value) : StronglyTypedUlid<InvoiceLineId>(Value);

// InvoiceTypes.cs
public enum InvoiceStatus
{
    Created,
    Paid
}

// InvoiceConfiguration.cs
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice> 
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptions.Default;

    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        // ✅ DO: Only configure EF mapping for complex types
        builder.MapStronglyTypedUuid<Invoice, InvoiceId>(i => i.Id);
        builder.MapStronglyTypedLongId<Invoice, TenantId>(t => t.TenantId);

        builder.OwnsOne(i => i.Address, b => b.ToJson()); // ✅ DO: Map 1:1 valueobjects and entites with .ToJson()

        // ✅ DO: Map collection with custom JsonSerializer
        builder.Property(i => i.InvoiceLines)
            .HasColumnName("InvoiceLines")
            .HasConversion(
                v => JsonSerializer.Serialize(v.ToArray(), JsonSerializerOptions),
                v => JsonSerializer.Deserialize<ImmutableArray<InvoiceLine>>(v, JsonSerializerOptions)
            );
    }
}

// ❌ Anti-patterns to avoid
public class BadInvoice : AggregateRoot<BadInvoiceId>
{
    // ❌ Public constructor
    public BadInvoice(InvoiceId id, CustomerId customerId) : base(id) { } // ❌ Generate Id outside
    // ❌ Public setters expose mutable state
    public string CustomerEmail { get; set; }
    // ❌ Direct reference to another aggregate
    public Customer Customer { get; set; }
    // ❌ Mutable collection exposed directly
    public List<BadInvoiceLine> InvoiceLines { get; set; } = new();
}

public sealed class BadInvoiceConfiguration : IEntityTypeConfiguration<BadInvoice> 
{
    public void Configure(EntityTypeBuilder<BadInvoice> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired(); // ❌ Do not configure primitive properties
        builder.Property(t => t.Description).HasMaxLength(255);  // ❌ Do not configure primitive properties
        builder.HasIndex(u => new { u.Email }).IsUnique();  // ❌ Is Unique is a database constraint, not need by EF Core at runtime
        builder.Property(u => u.Role).HasColumnType("varchar(10)").HasConversion<string>(); // ❌  EntityFramework is configured to convert all enums to string
    }
}
```
