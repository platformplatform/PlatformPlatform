using System.Collections.Immutable;
using System.Text.Json;
using Account.Features.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;

namespace Account.Features.SupportTickets.Domain;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptions.Default;

    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.MapStronglyTypedUuid<SupportTicket, SupportTicketId>(t => t.Id);
        builder.MapStronglyTypedLongId<SupportTicket, TenantId>(t => t.TenantId);
        builder.MapStronglyTypedUuid<SupportTicket, UserId>(t => t.ReporterId);

        // ShortDisplayId is a computed projection over the trailing characters of Id, not a stored column.
        builder.Ignore(t => t.ShortDisplayId);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).HasPrincipalKey(t => t.Id);

        // Assignee is mapped as a converted jsonb property rather than an owned ToJson() entity
        // because clearing it (unassigning a ticket) must persist a SQL NULL on update. EF Core does
        // not reliably write null back for an owned JSON reference, so unassign would silently no-op.
        builder.Property(t => t.Assignee)
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, JsonSerializerOptions),
                value => JsonSerializer.Deserialize<BackOfficeStaffRef>(value, JsonSerializerOptions)
            );

        builder.OwnsOne(t => t.Csat, b => b.ToJson());

        builder.Property(t => t.Messages)
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value.ToArray(), JsonSerializerOptions),
                value => JsonSerializer.Deserialize<ImmutableArray<SupportMessage>>(value, JsonSerializerOptions)
            )
            .Metadata.SetValueComparer(new ValueComparer<ImmutableArray<SupportMessage>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c
                )
            );

        builder.Property(t => t.HistoryEvents)
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value.ToArray(), JsonSerializerOptions),
                value => JsonSerializer.Deserialize<ImmutableArray<SupportTicketHistoryEvent>>(value, JsonSerializerOptions)
            )
            .Metadata.SetValueComparer(new ValueComparer<ImmutableArray<SupportTicketHistoryEvent>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c
                )
            );
    }
}
