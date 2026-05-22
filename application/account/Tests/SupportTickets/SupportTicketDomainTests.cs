using Account.Features.SupportTickets.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.SupportTickets;

public sealed class SupportTicketDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ComputeDisplayStatus_WhenStatusIsNew_ShouldPassThrough()
    {
        var ticket = CreateTicket();

        ticket.ComputeDisplayStatus(Now).Should().Be(SupportTicketStatus.New);
    }

    [Fact]
    public void ComputeDisplayStatus_WhenStatusIsAwaitingAgent_ShouldPassThrough()
    {
        var ticket = CreateTicket();
        ticket.PostUserMessage(ticket.ReporterId, "Need help.", [], Now);

        ticket.ComputeDisplayStatus(Now).Should().Be(SupportTicketStatus.AwaitingAgent);
    }

    [Fact]
    public void ComputeDisplayStatus_WhenStatusIsResolvedInsideWindowAndUnrated_ShouldRemainResolved()
    {
        var ticket = CreateTicket();
        ticket.MarkResolvedByUser(Now.AddDays(-2));

        ticket.ComputeDisplayStatus(Now).Should().Be(SupportTicketStatus.Resolved);
    }

    [Fact]
    public void ComputeDisplayStatus_WhenStatusIsResolvedPastReopenWindow_ShouldPromoteToClosed()
    {
        var ticket = CreateTicket();
        ticket.MarkResolvedByUser(Now - SupportTicket.ReopenWindowAfterResolved - TimeSpan.FromSeconds(1));

        ticket.ComputeDisplayStatus(Now).Should().Be(SupportTicketStatus.Closed);
    }

    [Fact]
    public void ComputeDisplayStatus_WhenStatusIsResolvedAndFreshCsatSubmitted_ShouldPromoteToClosed()
    {
        var ticket = CreateTicket();
        ticket.MarkResolvedByUser(Now.AddDays(-1));
        ticket.SubmitCsat(SupportTicketCsatScore.Helpful, "Worked great.", Now.AddMinutes(-5));

        ticket.ComputeDisplayStatus(Now).Should().Be(SupportTicketStatus.Closed);
    }

    [Fact]
    public void ComputeDisplayStatus_WhenStatusIsResolvedAndCsatIsStaleFromPriorReopen_ShouldRemainResolved()
    {
        // A reopen invalidates the prior CSAT. The reporter should be invited to re-rate, and the
        // bucket should stay Active until they do so or the reopen window closes again.
        var ticket = CreateTicket();
        ticket.MarkResolvedByUser(Now.AddDays(-5));
        ticket.SubmitCsat(SupportTicketCsatScore.Helpful, null, Now.AddDays(-4));
        ticket.ReopenByUser(Now.AddDays(-3));
        ticket.MarkResolvedByUser(Now.AddDays(-2));

        ticket.IsCsatStale().Should().BeTrue();
        ticket.ComputeDisplayStatus(Now).Should().Be(SupportTicketStatus.Resolved);
    }

    [Fact]
    public void IsCsatStale_WhenReopenInProgressBeforeReResolve_ShouldReturnFalse()
    {
        // Between reopen and re-resolve ResolvedAt is null, so IsCsatStale returns false. Consumers
        // (SubmitCsat, CloseTicketByUser, ComputeDisplayStatus) all gate on Status == Resolved/Closed
        // before consulting staleness, so the contract is not user-visible during this window.
        var ticket = CreateTicket();
        ticket.MarkResolvedByUser(Now.AddDays(-5));
        ticket.SubmitCsat(SupportTicketCsatScore.Helpful, null, Now.AddDays(-4));
        ticket.ReopenByUser(Now.AddDays(-3));

        ticket.IsCsatStale().Should().BeFalse();
    }

    // The legacy Status==Closed branch cannot be reached by any public aggregate method (the user
    // close path writes Resolved). It is exercised end-to-end by the API tests that seed a Closed row
    // directly via the test database helper. Leaving this branch domain-untested would be a gap, but
    // constructing the state via reflection here would be more misleading than the API coverage below.
    private static SupportTicket CreateTicket()
    {
        return SupportTicket.Create(
            new TenantId(1),
            UserId.NewId(),
            "Owner",
            "reporter@example.com",
            "Subject for domain test",
            SupportTicketCategory.Account,
            Now.AddDays(-10)
        );
    }
}
