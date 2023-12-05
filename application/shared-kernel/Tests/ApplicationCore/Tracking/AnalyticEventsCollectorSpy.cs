using PlatformPlatform.SharedKernel.ApplicationCore.Tracking;

namespace PlatformPlatform.SharedKernel.Tests.ApplicationCore.Tracking;

public class AnalyticEventsCollectorSpy(AnalyticEventsCollector realAnalyticEventsCollector) : IAnalyticEventsCollector
{
    private readonly List<AnalyticEvent> _collectedEvents = new();

    public IReadOnlyList<AnalyticEvent> CollectedEvents => _collectedEvents;

    public bool AreAllEventsDispatched { get; private set; }

    public void CollectEvent(string name, Dictionary<string, string>? properties = null)
    {
        realAnalyticEventsCollector.CollectEvent(name, properties);
        _collectedEvents.Add(new AnalyticEvent(name, properties));
    }

    public bool HasEvents => realAnalyticEventsCollector.HasEvents;

    public AnalyticEvent Dequeue()
    {
        var analyticEvent = realAnalyticEventsCollector.Dequeue();
        AreAllEventsDispatched = !realAnalyticEventsCollector.HasEvents;
        return analyticEvent;
    }

    public void Reset()
    {
        while (realAnalyticEventsCollector.HasEvents)
        {
            realAnalyticEventsCollector.Dequeue();
        }

        _collectedEvents.Clear();
        AreAllEventsDispatched = false;
    }
}