namespace PlatformPlatform.SharedKernel.Telemetry;

public interface ITelemetryEventsCollector
{
    bool HasEvents { get; }

    void CollectEvent(TelemetryEvent telemetryEvent);

    TelemetryEvent Dequeue();
}

public class TelemetryEventsCollector : ITelemetryEventsCollector
{
    private readonly Queue<TelemetryEvent> _events = new();

    public bool HasEvents => _events.Count > 0;

    public void CollectEvent(TelemetryEvent telemetryEvent)
    {
        _events.Enqueue(telemetryEvent);
    }

    public TelemetryEvent Dequeue()
    {
        return _events.Dequeue();
    }
}

public abstract class TelemetryEvent(params (string Key, object Value)[] properties)
{
    public Dictionary<string, string> Properties { get; } = properties.ToDictionary(p => $"event.{p.Key}", p => p.Value.ToString() ?? "");
}
