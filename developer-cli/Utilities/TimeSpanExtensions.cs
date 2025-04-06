namespace PlatformPlatform.DeveloperCli.Utilities;

public static class TimeSpanExtensions
{
    public static string Format(this TimeSpan timeSpan)
    {
        return timeSpan.TotalMinutes >= 1
            ? $"{timeSpan.TotalMinutes:N0}m {timeSpan.Seconds:N0}s"
            : $"{timeSpan.TotalSeconds:F1}s";
    }
}
