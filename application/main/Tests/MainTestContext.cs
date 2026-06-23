using Microsoft.Data.Sqlite;
using SharedKernel.Integrations.Email;
using SharedKernel.Tests.Telemetry;

namespace Main.Tests;

// Per-test state surfaced to the shared MainWebApplicationFactory via AsyncLocal so that each test sees
// its own database, telemetry collector, and email client substitute while the host stays shared across
// the test class.
public sealed class MainTestContext
{
    public required SqliteConnection Connection { get; init; }

    public required TelemetryEventsCollectorSpy TelemetryCollector { get; init; }

    public required IEmailClient EmailClient { get; init; }
}
