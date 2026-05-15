using Account.Integrations.Stripe;
using Microsoft.Data.Sqlite;
using SharedKernel.Tests.Telemetry;

namespace Account.Tests.BackOffice;

// Per-test state surfaced to the shared BackOfficeWebApplicationFactory via AsyncLocal so that
// each test sees its own database, telemetry collector, and Stripe state while the host stays
// shared across the test class.
public sealed class BackOfficeTestContext
{
    public required SqliteConnection Connection { get; init; }

    public required TelemetryEventsCollectorSpy TelemetryCollector { get; init; }

    public required MockStripeState StripeState { get; init; }
}
