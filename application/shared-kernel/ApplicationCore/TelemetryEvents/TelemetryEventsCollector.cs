namespace PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

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

public abstract class TelemetryEvent(string name, params (string Key, string Value)[] properties)
{
    public string Name { get; } = name;

    public Dictionary<string, string> Properties { get; } =
        properties.ToDictionary(p => $"Event_{p.Key}", p => p.Value);
}