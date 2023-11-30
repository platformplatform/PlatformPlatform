namespace PlatformPlatform.SharedKernel.ApplicationCore.Tracking;

public interface IAnalyticEventsCollector
{
    bool HasEvents { get; }

    void CollectEvent(string name, Dictionary<string, string>? properties = null);

    AnalyticEvent Dequeue();
}

public class AnalyticEventsCollector : IAnalyticEventsCollector
{
    private readonly Queue<AnalyticEvent> _events = new();

    public bool HasEvents => _events.Count > 0;

    public void CollectEvent(string name, Dictionary<string, string>? properties = null)
    {
        var analyticEvent = new AnalyticEvent(name, properties);
        _events.Enqueue(analyticEvent);
    }

    public AnalyticEvent Dequeue()
    {
        return _events.Dequeue();
    }
}

public class AnalyticEvent(string name, Dictionary<string, string>? properties = null)
{
    public string Name { get; } = name;

    public Dictionary<string, string>? Properties { get; } = properties;
}