using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.SharedKernel.Tests.Telemetry;

public class TelemetryEventsCollectorSpy(ITelemetryEventsCollector realTelemetryEventsCollector)
    : ITelemetryEventsCollector
{
    private readonly List<TelemetryEvent> _collectedEvents = new();

    public IReadOnlyList<TelemetryEvent> CollectedEvents => _collectedEvents;

    public bool AreAllEventsDispatched { get; private set; }

    public bool HasEvents => realTelemetryEventsCollector.HasEvents;

    public TelemetryEvent Dequeue()
    {
        var telemetryEvent = realTelemetryEventsCollector.Dequeue();
        AreAllEventsDispatched = !realTelemetryEventsCollector.HasEvents;
        return telemetryEvent;
    }

    public void CollectEvent(TelemetryEvent telemetryEvent)
    {
        realTelemetryEventsCollector.CollectEvent(telemetryEvent);
        _collectedEvents.Add(telemetryEvent);
    }

    public void Reset()
    {
        while (realTelemetryEventsCollector.HasEvents)
        {
            realTelemetryEventsCollector.Dequeue();
        }

        _collectedEvents.Clear();
        AreAllEventsDispatched = false;
    }
}
