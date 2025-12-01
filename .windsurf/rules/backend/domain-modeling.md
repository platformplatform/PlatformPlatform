---
trigger: glob
globs: **/Domain/*.cs
description: Rules for creating DDD aggregates, entities, value objects, and Entity Framework configuration
---

# Domain Modeling

Guidelines for implementing DDD models for aggregates, entities, and value objects.

## Implementation

1. Create all DDD models in `/[scs-name]/Core/Features/[Feature]/Domain`, including aggregates, entities, value objects, strongly typed IDs, repositories, and EF Core mapping
2. Understand the core DDD concepts:
   - Aggregates are the root of the DDD model and map 1:1 to database tables
   - Entities belong to aggregates but have their own identity
   - Value objects are immutable and have no identity
   - Repositories add, get, update, and remove aggregates—see [Repositories](/.windsurf/rules/backend/repositories.md)
3. Store entities and value objects as JSON columns on the Aggregate to avoid EF Core's `.Include()` method
4. For Aggregates:
   - Use public sealed classes that inherit from `AggregateRoot<TId>`
   - Create a strongly typed ID—see [Strongly Typed IDs](/.windsurf/rules/backend/strongly-typed-ids.md)
   - Never use navigational properties to other aggregates (e.g., no `User.Tenant` or `Order.Customer`)
   - Use factory methods when creating aggregates
   - Make properties private; use methods for state changes and enforcing business rules
   - Make properties immutable
   - For many-to-many aggregates, make them Tenant scoped (`ITenantScopedEntity`) even if FK constraints ensure isolation
   - For many-to-many aggregates, consider if cascade delete is needed—use `OnDelete(DeleteBehavior.Cascade)` if so
5. For Entities:
   - Use public sealed classes that inherit from `Entity<TId>`
   - Create a strongly typed ID
   - Use factory methods when creating entities
   - Use private setters to control state changes
   - Make properties private; use methods for state changes and enforcing business rules
   - Store entities as JSON columns on the Aggregate
6. For Value Objects:
   - Use records to ensure immutability
   - Value objects do not have an ID
7. Do NOT add Entity Framework configuration for primitive properties:
   - We don't use EF tooling for migrations, so no need for primitive property configuration (length, nullable)
   - Only configure EF properties for complex types (collections, value objects) that EF uses for generating SQL
8. When implementing a new aggregate, start with minimum required methods and add more as features require, ensuring each maintains aggregate invariants

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
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired(); // ❌ DON'T: Configure primitive properties
        builder.Property(t => t.Description).HasMaxLength(255);  // ❌ DON'T: Configure primitive properties
        builder.HasIndex(u => new { u.Email }).IsUnique();  // ❌ IsUnique is a database constraint, not needed by EF Core at runtime
        builder.Property(u => u.Role).HasColumnType("varchar(10)").HasConversion<string>(); // ❌ EF is configured to convert all enums to string

        // ❌ DON'T: Configure FK relationships without OnDelete - we don't use EF Tools for migrations
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(i => i.TenantId);

        // ✅ DO: Configure FK relationships only when you need cascade delete behavior at runtime
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Cascade); // EF needs this to handle cascades at runtime
    }
}
```
