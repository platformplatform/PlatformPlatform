namespace PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

public interface ITelemetryEventsCollector
{
    bool HasEvents { get; }

    void CollectEvent(string name, Dictionary<string, string>? properties = null);

    TelemetryEvent Dequeue();
}

public class TelemetryEventsCollector : ITelemetryEventsCollector
{
    private readonly Queue<TelemetryEvent> _events = new();

    public bool HasEvents => _events.Count > 0;

    public void CollectEvent(string name, Dictionary<string, string>? properties = null)
    {
        var telemetryEvent = new TelemetryEvent(name, properties);
        _events.Enqueue(telemetryEvent);
    }

    public TelemetryEvent Dequeue()
    {
        return _events.Dequeue();
    }
}

public class TelemetryEvent(string name, Dictionary<string, string>? properties = null)
{
    public string Name { get; } = name;

    public Dictionary<string, string>? Properties { get; } = properties;
}