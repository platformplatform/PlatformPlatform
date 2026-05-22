using Account.Features.SupportTickets.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.SupportTickets;

public sealed class SupportTicketRepositoryTests
{
    [Fact]
    public void GetByIdAsync_WhenCalledThroughInterface_ShouldDispatchToTenantFilteredOverride()
    {
        // SupportTicket is an ITenantScopedEntity, so GetByIdAsync must NOT resolve to
        // RepositoryBase.GetByIdAsync (which uses DbSet.FindAsync and bypasses every query filter,
        // including the tenant filter). The override on SupportTicketRepository uses
        // SingleOrDefaultAsync so the tenant filter applies. This test pins the interface dispatch so
        // a future refactor that removes the override fails loudly rather than silently reopening the
        // cross-tenant read surface.
        var interfaceMap = typeof(SupportTicketRepository).GetInterfaceMap(typeof(ICrudRepository<SupportTicket, SupportTicketId>));
        var interfaceMethod = typeof(ICrudRepository<SupportTicket, SupportTicketId>).GetMethod(nameof(ISupportTicketRepository.GetByIdAsync))!;
        var index = Array.IndexOf(interfaceMap.InterfaceMethods, interfaceMethod);

        var targetMethod = interfaceMap.TargetMethods[index];

        targetMethod.DeclaringType.Should().Be<SupportTicketRepository>();
    }
}
