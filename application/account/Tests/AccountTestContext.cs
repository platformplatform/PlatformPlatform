using Account.Integrations.Stripe;
using Microsoft.Data.Sqlite;
using SharedKernel.Integrations.Email;
using SharedKernel.Tests.Telemetry;

namespace Account.Tests;

// Per-test state surfaced to the shared AccountWebApplicationFactory via AsyncLocal so that each
// test sees its own database, telemetry collector, email client substitute, and Stripe state while
// the host stays shared across the test class.
public sealed class AccountTestContext
{
    public required SqliteConnection Connection { get; init; }

    public required TelemetryEventsCollectorSpy TelemetryCollector { get; init; }

    public required IEmailClient EmailClient { get; init; }

    public required MockStripeState StripeState { get; init; }
}
