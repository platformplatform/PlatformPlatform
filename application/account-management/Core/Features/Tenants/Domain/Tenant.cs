using PlatformPlatform.SharedKernel.Domain;
using System.Collections.Immutable;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Trial;
        StateHistory = ImmutableArray<TenantStateHistory>.Empty;
    }

    public string Name { get; private set; } = string.Empty;

    public TenantState State { get; private set; }

    public ImmutableArray<TenantStateHistory> StateHistory { get; private set; }

    public static Tenant Create(string email)
    {
        var tenant = new Tenant();
        tenant.RecordStateChange(email);
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }

    public void Update(string tenantName)
    {
        Name = tenantName;
    }

    public void ChangeState(TenantState newState, string changedByEmail)
    {
        if (newState == State) throw new UnreachableException($"Tenant is already in state '{newState}'.");

        State = newState;
        RecordStateChange(changedByEmail);
    }

    private void RecordStateChange(string mail)
    {
        var now = TimeProvider.System.GetUtcNow().DateTime;
        var newHistory = new TenantStateHistory(StateHistory.Length, now, State, mail);
        StateHistory = StateHistory.Add(newHistory);
    }
}

public sealed record TenantStateHistory(int Ordinal, DateTime ChangedAt, TenantState State, string ChangedByEmail);
