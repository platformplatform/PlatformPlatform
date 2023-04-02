namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public static class IdGenerator
{
    private static readonly object Lock = new();
    private static long _lastId;

    public static long NewId()
    {
        lock (Lock)
        {
            var newId = Math.Max(DateTime.UtcNow.Ticks, _lastId + 1);
            _lastId = newId;
            return newId;
        }
    }
}